using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto.Dataset;
using DataOrganizer.Extensions.Views;
using Shared.Extensions;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Behaviors.Scrolling;

/// <summary>
/// Keeps the place of the records in the associated <see cref="ScrollViewer" />: once enabled, brings back
/// <see cref="ViewState" /> and then reports the record at the top after each scroll; also scrolls to either edge.
/// A scroll that the user starts stops these moves.
/// </summary>
internal sealed class DatasetScrollBehavior : StyledElementBehavior<ScrollViewer>
{
	#region Properties
	/// <summary>
	/// The record at the top of the list and how far the list is scrolled into it.
	/// </summary>
	public DatasetEditorState? ViewState
	{
		get => GetValue(ViewStateProperty);
		set => SetValue(ViewStateProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="ViewState" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<DatasetEditorState?> ViewStateProperty = AvaloniaProperty
		.Register<DatasetScrollBehavior, DatasetEditorState?>(name: nameof(ViewState));
	#endregion

	#region Commands
	/// <summary>
	/// Scrolls the list to the end, stopping the scroll back to <see cref="ViewState" /> if it runs.
	/// </summary>
	public AsyncRelayCommand ScrollToEndCommand { get; }

	/// <summary>
	/// Scrolls the list to the top, stopping the scroll back to <see cref="ViewState" /> if it runs.
	/// </summary>
	public AsyncRelayCommand ScrollToTopCommand { get; }
	#endregion

	#region Data
	/// <summary>
	/// Subscription to <see cref="ScrollViewer.ScrollChanged" /> while the list is shown.
	/// </summary>
	private readonly SerialDisposable _scrollSubscription = new();

	/// <summary>
	/// Subscription to the scrolls that the user starts.
	/// </summary>
	private readonly SerialDisposable _userScrollSubscription = new();

	/// <summary>
	/// <c>True</c> once the behavior has brought back <see cref="ViewState" /> or is doing so.
	/// </summary>
	private bool _isStarted;

	/// <summary>
	/// Stops the scroll back to <see cref="ViewState" /> while it runs.
	/// </summary>
	private CancellationTokenSource? _scrollBack;
	#endregion

	#region Constructors
	static DatasetScrollBehavior()
	{
		// Nothing starts before the binding tells that the records are read.
		IsEnabledProperty.OverrideDefaultValue<DatasetScrollBehavior>(false);
	}

	public DatasetScrollBehavior()
	{
		ScrollToEndCommand = new(token => ScrollToEdgeAsync(toEnd: true, token), CanScrollToEnd);

		ScrollToTopCommand = new(token => ScrollToEdgeAsync(toEnd: false, token), CanScrollToTop);

		this
			.GetObservable(IsEnabledProperty)
			.Subscribe(IsEnabledProperty_Changed);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="StyledElementBehavior.IsEnabledProperty" /> changed handler.
	/// </summary>
	private void IsEnabledProperty_Changed(bool value)
	{
		if (!value)
		{
			return;
		}

		TryStart();
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		if (AssociatedObject is not { } scrollViewer)
		{
			return;
		}

		_userScrollSubscription.Disposable = WhenScrolledByUser(scrollViewer).Subscribe(_ => StopMoves());
	}

	/// <inheritdoc />
	protected override void OnAttachedToVisualTree()
	{
		base.OnAttachedToVisualTree();

		if (!_isStarted)
		{
			return;
		}

		SubscribeToScrollChanged();
	}

	/// <inheritdoc />
	protected override void OnDetachedFromVisualTree()
	{
		_scrollSubscription.Disposable = null;

		base.OnDetachedFromVisualTree();
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		_scrollSubscription.Disposable = null;

		_userScrollSubscription.Disposable = null;

		base.OnDetaching();
	}

	/// <inheritdoc />
	protected override void OnLoaded()
	{
		base.OnLoaded();

		TryStart();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when <paramref name="e" /> presses a scroll bar, of the list or of a control inside it.
	/// </summary>
	private static bool IsOnScrollBar(PointerPressedEventArgs e)
	{
		return e.Source is Visual source && source.FindAncestorOfType<ScrollBar>(includeSelf: true) is not null;
	}

	/// <summary>
	/// Smoothly scrolls <paramref name="scrollViewer" /> to the absolute top
	/// (<paramref name="toEnd" /> <c>False</c>) or bottom (<c>True</c>).
	/// </summary>
	private static Task SmoothScrollAsync(ScrollViewer scrollViewer, bool toEnd, CancellationToken token)
	{
		return StepOffsetUntilDoneAsync(scrollViewer, () =>
		{
			double maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

			double targetY = toEnd ? maxY : 0;

			return targetY - scrollViewer.Offset.Y;
		}, token);
	}

	/// <summary>
	/// Smoothly scrolls <paramref name="scrollViewer" /> so the record at
	/// <paramref name="topRecordIndex" /> sits with its top edge at
	/// <c>y = -<paramref name="withinRecordOffset" /></c> in viewport coordinates.
	/// </summary>
	private static async Task SmoothScrollAsync(
		ScrollViewer scrollViewer,
		ItemsRepeater container,
		int topRecordIndex,
		int totalRecords,
		double withinRecordOffset,
		CancellationToken token)
	{
		if (totalRecords <= 0)
		{
			return;
		}

		await StepOffsetUntilDoneAsync(scrollViewer, () =>
		{
			double maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

			double targetY = Math.Clamp(
				((double)topRecordIndex / totalRecords * scrollViewer.Extent.Height) - withinRecordOffset,
				0,
				maxY);

			return targetY - scrollViewer.Offset.Y;
		}, token).ConfigureAwait(true);

		double targetViewportY = -withinRecordOffset;

		await StepOffsetUntilDoneAsync(scrollViewer, () =>
		{
			// The records can change while the list moves, and a record that is gone ends the move.
			if (container.ItemsSourceView is not { } records || topRecordIndex >= records.Count)
			{
				return 0;
			}

			Control? child = container.TryGetElement(topRecordIndex) ?? container.GetOrCreateElement(topRecordIndex);

			if (child is null)
			{
				return 0;
			}

			container.UpdateLayout();

			if (child.TranslatePoint(default, scrollViewer) is not { } pointInViewport)
			{
				return 0;
			}

			return pointInViewport.Y - targetViewportY;
		}, token).ConfigureAwait(true);
	}

	/// <summary>
	/// Shared step-loop for the <c>SmoothScrollAsync</c> overloads.
	/// <paramref name="getRemainingDelta" /> returns signed pixels still to move
	/// (positive scrolls down); <paramref name="token" /> stops the loop at once.
	/// </summary>
	private static async Task StepOffsetUntilDoneAsync(
		ScrollViewer scrollViewer,
		Func<double> getRemainingDelta,
		CancellationToken token)
	{
		// Pixels moved per step. Smaller = more reliable realization, slower travel.
		const double stepPx = 100;

		// Pause between steps. Larger = more time for the layout pass to materialize items.
		const int delayMs = 16;

		// Safety cap against runaway loops if the offset never converges (e.g. extent keeps growing).
		const int maxIterations = 1000;

		for (int i = 0; i < maxIterations && !token.IsCancellationRequested; i++)
		{
			double delta = getRemainingDelta();

			if (Math.Abs(delta) < 0.5)
			{
				return;
			}

			double currentY = scrollViewer.Offset.Y;

			double step = Math.Sign(delta) * Math.Min(Math.Abs(delta), stepPx);

			double maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

			double newY = Math.Clamp(currentY + step, 0, maxY);

			if (Math.Abs(newY - currentY) < 0.5)
			{
				return;
			}

			scrollViewer.Offset = new Vector(scrollViewer.Offset.X, newY);

			await Task
				.Delay(delayMs, token)
				.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
		}
	}

	/// <summary>
	/// Returns the data index and pixel offset of the realized record whose top edge
	/// is the closest one at or above the <paramref name="scrollViewer" /> viewport
	/// top, or <c>null</c> if no realized child qualifies.
	/// </summary>
	private static (int Index, double WithinRecordOffset)? TryGetTopVisibleRecord(
		ItemsRepeater container,
		ScrollViewer scrollViewer)
	{
		int topIndex = -1;

		double bestViewportY = double.NegativeInfinity;

		foreach (Control child in container.Children)
		{
			int index = container.GetElementIndex(child);

			if (index < 0)
			{
				continue;
			}

			if (child.TranslatePoint(default, scrollViewer) is not { } pointInViewport)
			{
				continue;
			}

			double viewportY = pointInViewport.Y;

			if (viewportY <= 0 && viewportY > bestViewportY)
			{
				bestViewportY = viewportY;

				topIndex = index;
			}
		}

		return topIndex < 0 ? null : (topIndex, -bestViewportY);
	}

	/// <summary>
	/// Waits until <paramref name="container" /> has realized at least one child.
	/// </summary>
	private static async Task<bool> WaitForItemsRepeaterRealizedAsync(ItemsRepeater container)
	{
		Func<bool> condition = () => container
			.Children
			.Count > 0;

		await condition
			.WaitAsync(400, 10)
			.ConfigureAwait(true);

		return container
			.Children
			.Count > 0;
	}

	/// <summary>
	/// Signals each scroll that the user starts in <paramref name="scrollViewer" />: a turn of the mouse wheel, a press on
	/// a scroll bar or a page key.
	/// </summary>
	private static IObservable<Unit> WhenScrolledByUser(ScrollViewer scrollViewer)
	{
		// Seen on their way in, before the controls inside handle them.
		IObservable<Unit> wheel = scrollViewer
			.GetObservable(InputElement.PointerWheelChangedEvent, RoutingStrategies.Tunnel, handledEventsToo: true)
			.Select(static _ => Unit.Default);

		IObservable<Unit> scrollBar = scrollViewer
			.GetObservable(InputElement.PointerPressedEvent, RoutingStrategies.Tunnel, handledEventsToo: true)
			.Where(IsOnScrollBar)
			.Select(static _ => Unit.Default);

		IObservable<Unit> pageKeys = scrollViewer
			.GetObservable(InputElement.KeyDownEvent, RoutingStrategies.Tunnel, handledEventsToo: true)
			.Where(static x => x.Key is Key.PageDown or Key.PageUp)
			.Select(static _ => Unit.Default);

		return wheel
			.Merge(scrollBar)
			.Merge(pageKeys);
	}

	/// <summary>
	/// Validates <see cref="ScrollToEndCommand" />.
	/// </summary>
	private bool CanScrollToEnd() => !ScrollToTopCommand.IsRunning;

	/// <summary>
	/// Validates <see cref="ScrollToTopCommand" />.
	/// </summary>
	private bool CanScrollToTop() => !ScrollToEndCommand.IsRunning;

	/// <summary>
	/// Reports the record at the top of the list and how far the list is scrolled into it.
	/// </summary>
	private void ReportTopRecord()
	{
		if (!IsEnabled || AssociatedObject is not { Content: ItemsRepeater container } scrollViewer)
		{
			return;
		}

		if (TryGetTopVisibleRecord(container, scrollViewer) is not { } anchor)
		{
			return;
		}

		DatasetEditorState state = new()
		{
			TopRecordIndex = anchor.Index,
			WithinRecordOffset = anchor.WithinRecordOffset
		};

		SetCurrentValue(ViewStateProperty, state);
	}

	/// <summary>
	/// Brings back <see cref="ViewState" /> once the records are shown, unless <paramref name="token" /> stops it.
	/// </summary>
	private async Task ScrollBackAsync(ScrollViewer scrollViewer, ItemsRepeater container, CancellationToken token)
	{
		// A dataset that starts without records has nothing to realize and no position to restore.
		if (container.ItemsSourceView is not { Count: > 0 } records)
		{
			return;
		}

		await WaitForItemsRepeaterRealizedAsync(container).ConfigureAwait(true);

		// A record that the list does not have is not looked for, and the list starts at its top, which the first layout of
		// the records may have moved off.
		if (ViewState is not { } state || state.TopRecordIndex < 0 || state.TopRecordIndex >= records.Count)
		{
			scrollViewer.Offset = scrollViewer.Offset.WithY(0.0);

			return;
		}

		await SmoothScrollAsync(
			scrollViewer,
			container,
			state.TopRecordIndex,
			records.Count,
			state.WithinRecordOffset,
			token).ConfigureAwait(true);
	}

	/// <summary>
	/// Smoothly scrolls the list to the end (<paramref name="toEnd" /> <c>True</c>) or to the top, stopping the scroll back
	/// to <see cref="ViewState" /> if it runs.
	/// </summary>
	private Task ScrollToEdgeAsync(bool toEnd, CancellationToken token)
	{
		if (AssociatedObject is not { } scrollViewer)
		{
			return Task.CompletedTask;
		}

		_scrollBack?.Cancel();

		return SmoothScrollAsync(scrollViewer, toEnd, token);
	}

	/// <summary>
	/// Brings back <see cref="ViewState" /> once the records are shown, then follows the scroll.
	/// </summary>
	private async Task StartAsync(ScrollViewer scrollViewer, ItemsRepeater container)
	{
		using CancellationTokenSource scrollBack = new();

		_scrollBack = scrollBack;

		try
		{
			await ScrollBackAsync(scrollViewer, container, scrollBack.Token).ConfigureAwait(true);
		}
		finally
		{
			_scrollBack = null;
		}

		SubscribeToScrollChanged(isStopped: scrollBack.IsCancellationRequested);
	}

	/// <summary>
	/// Stops the scroll back to <see cref="ViewState" /> and the scroll to an edge, whichever runs.
	/// </summary>
	private void StopMoves()
	{
		_scrollBack?.Cancel();

		ScrollToEndCommand.Cancel();

		ScrollToTopCommand.Cancel();
	}

	/// <summary>
	/// Follows <see cref="ScrollViewer.ScrollChanged" /> of <see cref="StyledElementBehavior{T}.AssociatedObject" />,
	/// posted after the layout of the list, so the scroll that the layout itself makes is not reported;
	/// <paramref name="isStopped" /> also reports the place where the stopped scroll back left the list.
	/// </summary>
	private void SubscribeToScrollChanged(bool isStopped = false)
	{
		if (AssociatedObject is not { } scrollViewer)
		{
			return;
		}

		Dispatcher
			.UIThread
			.Post(() =>
			{
				// False for a layout-driven event (Extent/Viewport changed but not Offset), which is skipped.
				IObservable<bool> isScrolled = Observable
					.FromEventPattern<EventHandler<ScrollChangedEventArgs>, ScrollChangedEventArgs>(
						x => scrollViewer.ScrollChanged += x,
						x => scrollViewer.ScrollChanged -= x)
					.Select(static x => x.EventArgs.OffsetDelta != default);

				// The place where the list stopped is kept like the place after any scroll.
				if (isStopped)
				{
					isScrolled = isScrolled.StartWith(true);
				}

				_scrollSubscription.Disposable = isScrolled
					.SetDelay(TimeSpan.FromSeconds(0.5))
					.Where(static x => x)
					.Subscribe(_ => ReportTopRecord());
			}, DispatcherPriority.Loaded);
	}

	/// <summary>
	/// Starts the behavior once it is enabled and the list is shown.
	/// </summary>
	private void TryStart()
	{
		if (!IsEnabled
			|| _isStarted
			|| AssociatedObject is not { IsLoaded: true, Content: ItemsRepeater container } scrollViewer)
		{
			return;
		}

		_isStarted = true;

		_ = StartAsync(scrollViewer, container);
	}
	#endregion
}
