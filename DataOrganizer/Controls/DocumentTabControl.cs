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
using System.Collections.Specialized;
using System.Windows.Input;

namespace DataOrganizer.Controls;

/// <summary>
/// A <see cref="TabControl" /> of documents: a tab closes by its button or a middle click, a right click selects it,
/// and Ctrl+Tab goes back to the tab selected before.
/// </summary>
internal sealed class DocumentTabControl : TabControl
{
	#region Properties
	/// <summary>
	/// Command that closes a tab; it gets the item of the tab.
	/// </summary>
	public ICommand? CloseCommand
	{
		get => GetValue(CloseCommandProperty);
		set => SetValue(CloseCommandProperty, value);
	}

	/// <summary>
	/// Template of a tab header: the header built by <see cref="ItemsControl.ItemTemplate" /> with the close button.
	/// </summary>
	public IDataTemplate? TabHeaderTemplate
	{
		get => GetValue(TabHeaderTemplateProperty);
		set => SetValue(TabHeaderTemplateProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <inheritdoc cref="CloseCommand" />
	public static readonly StyledProperty<ICommand?> CloseCommandProperty = AvaloniaProperty
		.Register<DocumentTabControl, ICommand?>(nameof(CloseCommand));

	/// <inheritdoc cref="TabHeaderTemplate" />
	public static readonly StyledProperty<IDataTemplate?> TabHeaderTemplateProperty = AvaloniaProperty
		.Register<DocumentTabControl, IDataTemplate?>(nameof(TabHeaderTemplate));
	#endregion

	#region Data
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
		ItemsView.CollectionChanged += ItemsView_CollectionChanged;

		// Subscribed after the base class, whose own handler selects the first tab.
		Selection.LostSelection += Selection_LostSelection;

		KeyBindings.Add(new KeyBinding
		{
			Command = new RelayCommand(SwitchToPreviousTab),
			Gesture = new KeyGesture(Key.Tab, KeyModifiers.Control)
		});
	}
	#endregion

	#region Event Handlers
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
