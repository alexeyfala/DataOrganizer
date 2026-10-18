using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using DataOrganizer.Helpers;
using Shared.Extensions;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DataOrganizer.Behaviors.Hierarchy;

/// <summary>
/// Highlights the objects of the hierarchy that the attached tree or list shows, and brings them into view and selects
/// them first when they are to be shown, as the signals of the view model ask.
/// </summary>
internal sealed class ExplorerItemSignalsBehavior : StyledElementBehavior<ItemsControl>
{
	#region Data
	/// <summary>
	/// Pause between the selection of a shown object and its highlight, and between the checks whether the window has
	/// loaded.
	/// </summary>
	private const int ShowDelay = 200;

	/// <summary>
	/// Stops the waits of the shows once the behavior is detached.
	/// </summary>
	private readonly CancellationTokenSource _cancellation = new();

	/// <summary>
	/// Subscriptions to the signals.
	/// </summary>
	private readonly CompositeDisposable _subscriptions = [];
	#endregion

	#region Properties
	/// <summary>
	/// Objects to highlight.
	/// </summary>
	public IObservable<object>? HighlightSignal
	{
		get => GetValue(HighlightSignalProperty);
		set => SetValue(HighlightSignalProperty, value);
	}

	/// <summary>
	/// Command that selects a shown object; its parameter is the object.
	/// </summary>
	public ICommand? SelectCommand
	{
		get => GetValue(SelectCommandProperty);
		set => SetValue(SelectCommandProperty, value);
	}

	/// <summary>
	/// Objects to bring into view, select and highlight, once the window has loaded.
	/// </summary>
	public IObservable<object>? ShowSignal
	{
		get => GetValue(ShowSignalProperty);
		set => SetValue(ShowSignalProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <inheritdoc cref="HighlightSignal" />
	public static readonly StyledProperty<IObservable<object>?> HighlightSignalProperty = AvaloniaProperty
		.Register<ExplorerItemSignalsBehavior, IObservable<object>?>(nameof(HighlightSignal));

	/// <inheritdoc cref="SelectCommand" />
	public static readonly StyledProperty<ICommand?> SelectCommandProperty = AvaloniaProperty
		.Register<ExplorerItemSignalsBehavior, ICommand?>(nameof(SelectCommand));

	/// <inheritdoc cref="ShowSignal" />
	public static readonly StyledProperty<IObservable<object>?> ShowSignalProperty = AvaloniaProperty
		.Register<ExplorerItemSignalsBehavior, IObservable<object>?>(nameof(ShowSignal));
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		this
			.GetObservable(HighlightSignalProperty)
			.Select(static x => x ?? Observable.Never<object>())
			.Switch()
			.Subscribe(Highlight)
			.DisposeWith(_subscriptions);

		this
			.GetObservable(ShowSignalProperty)
			.Select(static x => x ?? Observable.Never<object>())
			.Switch()
			.Subscribe(x => _ = ShowAsync(x, _cancellation.Token))
			.DisposeWith(_subscriptions);
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		_subscriptions.Dispose();

		_cancellation.Cancel();

		_cancellation.Dispose();

		base.OnDetaching();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the container that shows an item, at any depth of a tree; <c>null</c> while it shows none.
	/// </summary>
	private TemplatedControl? FindContainer(object item)
	{
		return AssociatedObject switch
		{
			TreeView tree => tree.TreeContainerFromItem(item) as TemplatedControl,
			{ } items => items.ContainerFromItem(item) as TemplatedControl,
			null => null
		};
	}

	/// <summary>
	/// Highlights the container that shows an item.
	/// </summary>
	private void Highlight(object item)
	{
		if (FindContainer(item) is not { } container)
		{
			return;
		}

		_ = BrushAnimations.ApplyHighlightAnimationAsync(() => container.Background as Brush);
	}

	/// <summary>
	/// Brings an item into view, selects it and highlights it once the window has loaded.
	/// </summary>
	private async Task ShowAsync(object item, CancellationToken token)
	{
		if (AssociatedObject is not { } items)
		{
			return;
		}

		// A selection made before the window has loaded would not be saved.
		Func<bool> isLoaded = () => TopLevel.GetTopLevel(items) is { IsLoaded: true };

		if (!await isLoaded
			.WaitAsync(ShowDelay, 10, token)
			.ConfigureAwait(true))
		{
			return;
		}

		items.ScrollIntoView(item);

		if (items.FindDescendantOfType<ScrollViewer>() is { } scrollViewer)
		{
			scrollViewer.Offset = new(
				int.MaxValue,
				scrollViewer.Offset.Y);
		}

		SelectCommand?.Execute(item);

		if (FindContainer(item) is not { } container)
		{
			return;
		}

		await Task
			.Delay(ShowDelay, token)
			.ConfigureAwait(true);

		await BrushAnimations.ApplyHighlightAnimationAsync(
			() => container.Background as Brush,
			token).ConfigureAwait(false);
	}
	#endregion
}
