using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
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
	/// Scrolls the list to the end.
	/// </summary>
	public AsyncRelayCommand ScrollToEndCommand { get; }

	/// <summary>
	/// Scrolls the list to the top.
	/// </summary>
	public AsyncRelayCommand ScrollToTopCommand { get; }
	#endregion

	#region Data
	/// <summary>
	/// Subscription to <see cref="ScrollViewer.ScrollChanged" /> while the list is shown.
	/// </summary>
	private readonly SerialDisposable _scrollSubscription = new();

	/// <summary>
	/// <c>True</c> once the behavior has brought back <see cref="ViewState" /> or is doing so.
	/// </summary>
	private bool _isStarted;
	#endregion

	#region Constructors
	static DatasetScrollBehavior()
	{
		// Nothing starts before the binding tells that the records are read.
		IsEnabledProperty.OverrideDefaultValue<DatasetScrollBehavior>(false);
	}

	public DatasetScrollBehavior()
	{
		ScrollToEndCommand = new(() => ScrollToEdgeAsync(toEnd: true), CanScrollToEnd);

		ScrollToTopCommand = new(() => ScrollToEdgeAsync(toEnd: false), CanScrollToTop);

		this
			.GetObservable(IsEnabledProperty)
			.Subscribe(IsEnabledProperty_Changed);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="ScrollViewer.ScrollChanged" /> handler of <see cref="StyledElementBehavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_ScrollChanged(EventPattern<ScrollChangedEventArgs> e)
	{
		if (!IsEnabled || e.Sender is not ScrollViewer { Content: ItemsRepeater container } scrollViewer)
		{
			return;
		}

		// Skip layout-driven events (Extent/Viewport changed but not Offset).
		if (e.EventArgs.OffsetDelta == default)
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
	/// Smoothly scrolls <paramref name="scrollViewer" /> to the absolute top
	/// (<paramref name="toEnd" /> <c>False</c>) or bottom (<c>True</c>).
	/// </summary>
	private static Task SmoothScrollAsync(ScrollViewer scrollViewer, bool toEnd)
	{
		return StepOffsetUntilDoneAsync(scrollViewer, () =>
		{
			double maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

			double targetY = toEnd ? maxY : 0;

			return targetY - scrollViewer.Offset.Y;
		});
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
		double withinRecordOffset)
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
		}).ConfigureAwait(true);

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
		}).ConfigureAwait(true);
	}

	/// <summary>
	/// Shared step-loop for the <c>SmoothScrollAsync</c> overloads.
	/// <paramref name="getRemainingDelta" /> returns signed pixels still to move
	/// (positive scrolls down).
	/// </summary>
	private static async Task StepOffsetUntilDoneAsync(ScrollViewer scrollViewer, Func<double> getRemainingDelta)
	{
		// Pixels moved per step. Smaller = more reliable realization, slower travel.
		const double stepPx = 100;

		// Pause between steps. Larger = more time for the layout pass to materialize items.
		const int delayMs = 16;

		// Safety cap against runaway loops if the offset never converges (e.g. extent keeps growing).
		const int maxIterations = 1000;

		for (int i = 0; i < maxIterations; i++)
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
				.Delay(delayMs)
				.ConfigureAwait(true);
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
	private static async Task<bool> WaitForItemsRepeaterRealizedAsync(
		ItemsRepeater container,
		CancellationToken token = default)
	{
		Func<bool> condition = () => container
			.Children
			.Count > 0;

		await condition
			.WaitAsync(400, 10, token)
			.ConfigureAwait(true);

		return container
			.Children
			.Count > 0;
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
	/// Smoothly scrolls the list to the end (<paramref name="toEnd" /> <c>True</c>) or to the top.
	/// </summary>
	private Task ScrollToEdgeAsync(bool toEnd)
	{
		if (AssociatedObject is not { } scrollViewer)
		{
			return Task.CompletedTask;
		}

		return SmoothScrollAsync(scrollViewer, toEnd);
	}

	/// <summary>
	/// Brings back <see cref="ViewState" /> once the records are shown, then follows the scroll.
	/// </summary>
	private async Task StartAsync(ScrollViewer scrollViewer, ItemsRepeater container)
	{
		// A dataset that starts without records has nothing to realize and no position to restore,
		// yet the position it gains once records are added still has to be kept.
		if (container.ItemsSourceView is { Count: > 0 } records)
		{
			await WaitForItemsRepeaterRealizedAsync(container).ConfigureAwait(true);

			// A record that the list does not have is not looked for.
			if (ViewState is { } state && state.TopRecordIndex >= 0 && state.TopRecordIndex < records.Count)
			{
				await SmoothScrollAsync(
					scrollViewer,
					container,
					state.TopRecordIndex,
					records.Count,
					state.WithinRecordOffset).ConfigureAwait(true);
			}
		}

		SubscribeToScrollChanged();
	}

	/// <summary>
	/// Follows <see cref="ScrollViewer.ScrollChanged" /> of <see cref="StyledElementBehavior{T}.AssociatedObject" />,
	/// posted after the layout of the list, so the scroll that the layout itself makes is not reported.
	/// </summary>
	private void SubscribeToScrollChanged()
	{
		if (AssociatedObject is not { } scrollViewer)
		{
			return;
		}

		Dispatcher.UIThread.Post(() =>
		{
			_scrollSubscription.Disposable = Observable.FromEventPattern<EventHandler<ScrollChangedEventArgs>, ScrollChangedEventArgs>(
				x => scrollViewer.ScrollChanged += x,
				x => scrollViewer.ScrollChanged -= x)
				.SetDelay(TimeSpan.FromSeconds(0.5))
				.Subscribe(AssociatedObject_ScrollChanged);
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
