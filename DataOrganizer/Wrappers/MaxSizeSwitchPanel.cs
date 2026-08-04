using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Specialized;

namespace DataOrganizer.Wrappers;

/// <summary>
/// A panel that displays a single child selected by <see cref="SelectedIndex" /> while sizing
/// itself to the largest child, so switching the selection keeps the layout size unchanged.
/// </summary>
internal sealed class MaxSizeSwitchPanel : Panel
{
	#region Properties
	/// <summary>
	/// Index of the child to display.
	/// </summary>
	public int SelectedIndex
	{
		get => GetValue(SelectedIndexProperty);
		set => SetValue(SelectedIndexProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="SelectedIndex" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<int> SelectedIndexProperty = AvaloniaProperty
		.Register<MaxSizeSwitchPanel, int>(name: nameof(SelectedIndex));
	#endregion

	#region Constructors
	public MaxSizeSwitchPanel()
	{
		// Keeps the children placed outside the panel bounds invisible.
		ClipToBounds = true;

		Children.CollectionChanged += Children_CollectionChanged;
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="Panel.Children" /> <see cref="INotifyCollectionChanged.CollectionChanged" /> handler.
	/// </summary>
	private void Children_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateChildrenState();
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override Size ArrangeOverride(Size finalSize)
	{
		for (int index = 0; index < Children.Count; index++)
		{
			Control child = Children[index];

			// The unselected children are placed left of the origin instead of being collapsed,
			// so that they keep taking part in measuring.
			child.Arrange(index == SelectedIndex
				? new Rect(finalSize)
				: new Rect(
					new Point(-child.DesiredSize.Width - 1.0, 0.0),
					child.DesiredSize));
		}

		return finalSize;
	}

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		double width = 0.0;

		double height = 0.0;

		foreach (Control child in Children)
		{
			child.Measure(availableSize);

			width = Math.Max(width, child.DesiredSize.Width);

			height = Math.Max(height, child.DesiredSize.Height);
		}

		return new(width, height);
	}

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property != SelectedIndexProperty)
		{
			return;
		}

		UpdateChildrenState();

		InvalidateArrange();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Leaves only the selected child enabled: a disabled child is skipped by keyboard
	/// navigation and hit testing, but is still measured.
	/// </summary>
	private void UpdateChildrenState()
	{
		for (int index = 0; index < Children.Count; index++)
		{
			Children[index].IsEnabled = index == SelectedIndex;
		}
	}
	#endregion
}
