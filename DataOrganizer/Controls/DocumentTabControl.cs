using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;

namespace DataOrganizer.Controls;

/// <summary>
/// A <see cref="TabControl" /> of documents: a button after the tabs adds one, a tab is dragged along the row, closes
/// by its button, a middle click or its menu, a right click selects it and opens the menu, and Ctrl+Tab goes back to
/// the tab selected before.
/// </summary>
internal sealed class DocumentTabControl : TabControl
{
	#region Properties
	/// <summary>
	/// Command that adds a tab; the button after the tabs shows only while it is set.
	/// </summary>
	public ICommand? AddCommand
	{
		get => GetValue(AddCommandProperty);
		set => SetValue(AddCommandProperty, value);
	}

	/// <summary>
	/// Template of the items a place adds at the bottom of the menu of a tab; it gets the item of the tab.
	/// </summary>
	public IDataTemplate? AdditionalMenuItemsTemplate
	{
		get => GetValue(AdditionalMenuItemsTemplateProperty);
		set => SetValue(AdditionalMenuItemsTemplateProperty, value);
	}

	/// <summary>
	/// Command that closes every tab.
	/// </summary>
	public ICommand CloseAllTabsCommand { get; }

	/// <summary>
	/// Command that closes a tab; it gets the item of the tab.
	/// </summary>
	public ICommand? CloseCommand
	{
		get => GetValue(CloseCommandProperty);
		set => SetValue(CloseCommandProperty, value);
	}

	/// <summary>
	/// Command that closes every tab but the one whose item it gets.
	/// </summary>
	public ICommand CloseOtherTabsCommand { get; }

	/// <summary>
	/// Template of a tab header: the header built by <see cref="ItemsControl.ItemTemplate" /> with the close button.
	/// </summary>
	public IDataTemplate? TabHeaderTemplate
	{
		get => GetValue(TabHeaderTemplateProperty);
		set => SetValue(TabHeaderTemplateProperty, value);
	}

	/// <summary>
	/// Template of the menu of a tab: the items that close tabs, then <see cref="AdditionalMenuItemsTemplate" />.
	/// </summary>
	public IDataTemplate? TabMenuTemplate
	{
		get => GetValue(TabMenuTemplateProperty);
		set => SetValue(TabMenuTemplateProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <inheritdoc cref="AddCommand" />
	public static readonly StyledProperty<ICommand?> AddCommandProperty = AvaloniaProperty
		.Register<DocumentTabControl, ICommand?>(nameof(AddCommand));

	/// <inheritdoc cref="AdditionalMenuItemsTemplate" />
	public static readonly StyledProperty<IDataTemplate?> AdditionalMenuItemsTemplateProperty = AvaloniaProperty
		.Register<DocumentTabControl, IDataTemplate?>(nameof(AdditionalMenuItemsTemplate));

	/// <inheritdoc cref="CloseCommand" />
	public static readonly StyledProperty<ICommand?> CloseCommandProperty = AvaloniaProperty
		.Register<DocumentTabControl, ICommand?>(nameof(CloseCommand));

	/// <inheritdoc cref="TabHeaderTemplate" />
	public static readonly StyledProperty<IDataTemplate?> TabHeaderTemplateProperty = AvaloniaProperty
		.Register<DocumentTabControl, IDataTemplate?>(nameof(TabHeaderTemplate));

	/// <inheritdoc cref="TabMenuTemplate" />
	public static readonly StyledProperty<IDataTemplate?> TabMenuTemplateProperty = AvaloniaProperty
		.Register<DocumentTabControl, IDataTemplate?>(nameof(TabMenuTemplate));
	#endregion

	#region Data
	/// <summary>
	/// Tab the left button was pressed on, which follows the pointer along the row while the button is down.
	/// </summary>
	private TabItem? _draggedTab;

	/// <summary>
	/// Point of <see cref="_draggedTab" /> the pointer grabbed it at.
	/// </summary>
	private Point _grabPoint;

	/// <summary>
	/// Tab the middle button was pressed on.
	/// </summary>
	private TabItem? _middlePressedTab;

	/// <summary>
	/// Item that was selected before the current one.
	/// </summary>
	private object? _previousItem;

	/// <summary>
	/// Index of the item removed by the last change of the items, or -1.
	/// </summary>
	private int _removedIndex = -1;
	#endregion

	#region Constructors
	public DocumentTabControl()
	{
		CloseAllTabsCommand = new RelayCommand(CloseAllTabs);

		CloseOtherTabsCommand = new RelayCommand<object?>(CloseOtherTabs, _ => ItemCount > 1);

		ItemsView.CollectionChanged += ItemsView_CollectionChanged;

		// Subscribed after the base class, whose own handler selects the first tab.
		Selection.LostSelection += Selection_LostSelection;

		AddHandler(ContextRequestedEvent, DocumentTabControl_ContextRequested);

		// A tab takes the press of the left button to select itself, so the press comes here already handled.
		AddHandler(PointerPressedEvent, DocumentTabControl_PointerPressed, handledEventsToo: true);

		KeyBindings.Add(new KeyBinding
		{
			Command = new RelayCommand(SwitchToPreviousTab),
			Gesture = new KeyGesture(Key.Tab, KeyModifiers.Control)
		});
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.ContextRequestedEvent" /> handler of the control.
	/// </summary>
	private void DocumentTabControl_ContextRequested(object? sender, ContextRequestedEventArgs e)
	{
		if (e.Handled
			|| GetTabItem(e.Source) is not { } tab
			|| TabMenuTemplate?.Build(tab.DataContext) is not { } content)
		{
			return;
		}

		new Flyout
		{
			Content = content,
			OverlayDismissEventPassThrough = true
		}.ShowAt(tab, showAtPointer: true);

		e.Handled = true;
	}

	/// <summary>
	/// <see cref="InputElement.PointerPressedEvent" /> handler of the control.
	/// </summary>
	private void DocumentTabControl_PointerPressed(object? sender, PointerPressedEventArgs e)
	{
		// A button of a tab, such as the close one, does not drag the tab.
		if (e.GetCurrentPoint(this).Properties.PointerUpdateKind is not PointerUpdateKind.LeftButtonPressed
			|| e.Source is not Visual source
			|| source.FindAncestorOfType<Button>(includeSelf: true) is not null
			|| GetTabItem(source) is not { } tab)
		{
			return;
		}

		_draggedTab = tab;

		_grabPoint = e.GetPosition(tab);
	}

	/// <summary>
	/// <see cref="INotifyCollectionChanged.CollectionChanged" /> event handler of <see cref="ItemsControl.ItemsView" />.
	/// </summary>
	private void ItemsView_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		_removedIndex = e.Action == NotifyCollectionChangedAction.Remove
			? e.OldStartingIndex
			: -1;
	}

	/// <summary>
	/// <see cref="ISelectionModel.LostSelection" /> event handler of <see cref="SelectingItemsControl.Selection" />.
	/// </summary>
	private void Selection_LostSelection(object? sender, EventArgs e)
	{
		int removedIndex = _removedIndex;

		_removedIndex = -1;

		// A closed tab gives way to its right neighbour, or to the left one at the end of the row.
		if (removedIndex < 0 || ItemCount <= 0)
		{
			return;
		}

		SelectedIndex = Math.Min(removedIndex, ItemCount - 1);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
	{
		base.OnPointerCaptureLost(e);

		// The pointer lets the tab go with its button, or when anything else takes it.
		_draggedTab = null;
	}

	/// <inheritdoc />
	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);

		if (_draggedTab is not { } tab
			|| ItemsPanelRoot is not { } panel
			|| ItemsSource is not IList { IsFixedSize: false } items)
		{
			return;
		}

		// Where the tab would stand if it followed the pointer.
		double left = e.GetPosition(panel).X - _grabPoint.X;

		MoveNeighbours(items, tab, left);
	}

	/// <inheritdoc />
	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);

		_middlePressedTab = e.GetCurrentPoint(this).Properties.PointerUpdateKind
			is PointerUpdateKind.MiddleButtonPressed
			? GetTabItem(e.Source)
			: null;
	}

	/// <inheritdoc />
	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);

		TabItem? pressedTab = _middlePressedTab;

		_middlePressedTab = null;

		// The press and the release both have to happen on the same tab.
		if (pressedTab is null
			|| e.InitialPressMouseButton is not MouseButton.Middle
			|| !ReferenceEquals(GetTabItem(e.Source), pressedTab))
		{
			return;
		}

		if (pressedTab.DataContext is not { } item
			|| CloseCommand is not { } command
			|| !command.CanExecute(item))
		{
			return;
		}

		command.Execute(item);

		e.Handled = true;
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property != SelectedItemProperty)
		{
			return;
		}

		// A closed tab is no way back.
		if (change.OldValue is { } oldItem && ItemsView.Contains(oldItem))
		{
			_previousItem = oldItem;
		}

		// Keeps Ctrl+Tab at hand after a tab is selected from elsewhere, such as a newly opened one.
		Dispatcher.UIThread.Post(FocusSelectedTab, DispatcherPriority.Loaded);
	}

	/// <inheritdoc />
	protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
	{
		base.PrepareContainerForItemOverride(container, item, index);

		if (container is not TabItem tabItem || TabHeaderTemplate is not { } template)
		{
			return;
		}

		tabItem.HeaderTemplate = template;
	}

	/// <inheritdoc />
	protected override bool ShouldTriggerSelection(Visual selectable, PointerEventArgs eventArgs)
	{
		// A context menu always acts on the tab it was invoked from.
		return base.ShouldTriggerSelection(selectable, eventArgs)
		|| (eventArgs.Properties.PointerUpdateKind is PointerUpdateKind.RightButtonPressed
			&& ItemSelectionEventTriggers.ShouldTriggerSelection(selectable, eventArgs));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Moves the item at <paramref name="oldIndex" /> of a list to <paramref name="newIndex" />.
	/// </summary>
	private static void MoveItem(IList items, int oldIndex, int newIndex)
	{
		object? item = items[oldIndex];

		items.RemoveAt(oldIndex);

		items.Insert(newIndex, item);
	}

	/// <summary>
	/// Closes every tab by <see cref="CloseCommand" />.
	/// </summary>
	private void CloseAllTabs() => CloseTabs(ItemsView);

	/// <summary>
	/// Closes every tab but the one of <paramref name="item" /> by <see cref="CloseCommand" />.
	/// </summary>
	private void CloseOtherTabs(object? item) => CloseTabs(ItemsView.Where(x => !ReferenceEquals(x, item)));

	/// <summary>
	/// Closes the tabs of <paramref name="items" /> by <see cref="CloseCommand" />.
	/// </summary>
	private void CloseTabs(IEnumerable<object?> items)
	{
		if (CloseCommand is not { } command)
		{
			return;
		}

		// Each close changes the items, so the loop goes over a copy.
		foreach (object? item in items.ToArray())
		{
			if (!command.CanExecute(item))
			{
				continue;
			}

			command.Execute(item);
		}
	}

	/// <summary>
	/// Moves the keyboard focus to the selected tab.
	/// </summary>
	private void FocusSelectedTab()
	{
		if (SelectedItem is not { } item || ContainerFromItem(item) is not { } tab)
		{
			return;
		}

		tab.Focus();
	}

	/// <summary>
	/// Finds the tab of this control the event source belongs to.
	/// </summary>
	private TabItem? GetTabItem(object? source)
	{
		if (source is not Visual visual
			|| visual.FindAncestorOfType<TabItem>(includeSelf: true) is not { } tabItem)
		{
			return null;
		}

		return ReferenceEquals(tabItem.FindAncestorOfType<TabControl>(), this)
			? tabItem
			: null;
	}

	/// <summary>
	/// Moves the neighbours of a dragged tab across it while the tab, standing at <paramref name="left" />, goes over
	/// their middles; the dragged tab itself stays in the items, so it keeps its selection and container.
	/// </summary>
	private void MoveNeighbours(IList items, TabItem tab, double left)
	{
		int index = IndexFromContainer(tab);

		if (index < 0)
		{
			return;
		}

		// A moved neighbour gets a new container, which has no place in the row until the row is laid out.
		while (ContainerFromIndex(index + 1) is { } next && left + tab.Bounds.Width > next.Bounds.Center.X)
		{
			MoveItem(items, index + 1, index);

			index++;

			UpdateLayout();
		}

		while (ContainerFromIndex(index - 1) is { } previous && left < previous.Bounds.Center.X)
		{
			MoveItem(items, index - 1, index);

			index--;

			UpdateLayout();
		}
	}

	/// <summary>
	/// Selects the tab that was selected before the current one, while it is open.
	/// </summary>
	private void SwitchToPreviousTab()
	{
		if (_previousItem is not { } item || !ItemsView.Contains(item))
		{
			return;
		}

		SelectedItem = item;
	}
	#endregion
}
