using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using DataOrganizer.Dto.Entities;
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
	public IObservable<ExplorerItemDtoBase>? HighlightSignal
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
	public IObservable<ExplorerItemDtoBase>? ShowSignal
	{
		get => GetValue(ShowSignalProperty);
		set => SetValue(ShowSignalProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <inheritdoc cref="HighlightSignal" />
	public static readonly StyledProperty<IObservable<ExplorerItemDtoBase>?> HighlightSignalProperty = AvaloniaProperty
		.Register<ExplorerItemSignalsBehavior, IObservable<ExplorerItemDtoBase>?>(nameof(HighlightSignal));

	/// <inheritdoc cref="SelectCommand" />
	public static readonly StyledProperty<ICommand?> SelectCommandProperty = AvaloniaProperty
		.Register<ExplorerItemSignalsBehavior, ICommand?>(nameof(SelectCommand));

	/// <inheritdoc cref="ShowSignal" />
	public static readonly StyledProperty<IObservable<ExplorerItemDtoBase>?> ShowSignalProperty = AvaloniaProperty
		.Register<ExplorerItemSignalsBehavior, IObservable<ExplorerItemDtoBase>?>(nameof(ShowSignal));
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		this
			.GetObservable(HighlightSignalProperty)
			.Select(static x => x ?? Observable.Never<ExplorerItemDtoBase>())
			.Switch()
			.Subscribe(Highlight)
			.DisposeWith(_subscriptions);

		this
			.GetObservable(ShowSignalProperty)
			.Select(static x => x ?? Observable.Never<ExplorerItemDtoBase>())
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
	/// Finds the deepest container of the folders of an object, starting from <paramref name="container" />.
	/// </summary>
	private static ItemsControl FindLastContainer(ItemsControl container, FolderDto[] parents)
	{
		if (parents.IsEmpty() || container.ContainerFromItem(parents[0]) is not ItemsControl item)
		{
			return container;
		}

		return FindLastContainer(item, [.. parents.Skip(1)]);
	}

	/// <summary>
	/// Returns the container that shows an object; <c>null</c> while it shows none.
	/// </summary>
	private TemplatedControl? FindContainer(ExplorerItemDtoBase dto)
	{
		if (AssociatedObject is not { } items)
		{
			return null;
		}

		FolderDto[] parents = [.. dto
			.GetAllParents()
			.Reverse()];

		return FindLastContainer(items, parents).ContainerFromItem(dto) as TemplatedControl;
	}

	/// <summary>
	/// Highlights the container that shows an object.
	/// </summary>
	private void Highlight(ExplorerItemDtoBase dto)
	{
		if (FindContainer(dto) is not { } container)
		{
			return;
		}

		_ = BrushAnimations.ApplyHighlightAnimationAsync(() => container.Background as Brush);
	}

	/// <summary>
	/// Brings an object into view, selects it and highlights it once the window has loaded.
	/// </summary>
	private async Task ShowAsync(ExplorerItemDtoBase dto, CancellationToken token)
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

		items.ScrollIntoView(dto);

		if (items.FindDescendantOfType<ScrollViewer>() is { } scrollViewer)
		{
			scrollViewer.Offset = new(
				int.MaxValue,
				scrollViewer.Offset.Y);
		}

		SelectCommand?.Execute(dto);

		if (FindContainer(dto) is not { } container)
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
