using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Resizes a target through <see cref="Width" /> / <see cref="Height" /> by accumulating
/// <see cref="Thumb.DragDelta" /> increments of the attached <see cref="Thumb" />, towards <see cref="Edge" />.
/// </summary>
internal sealed class DragResizeBehavior : Behavior<Thumb>
{
	#region Properties
	/// <summary>
	/// Edge (or corner) the resize is performed towards.
	/// </summary>
	public WindowEdge Edge
	{
		get => GetValue(EdgeProperty);
		set => SetValue(EdgeProperty, value);
	}

	/// <summary>
	/// Resized height. Bound two-way to the target's height.
	/// </summary>
	public double Height
	{
		get => GetValue(HeightProperty);
		set => SetValue(HeightProperty, value);
	}

	/// <summary>
	/// Minimum size below which a resize step is ignored (per affected axis).
	/// </summary>
	public double MinimumSize
	{
		get => GetValue(MinimumSizeProperty);
		set => SetValue(MinimumSizeProperty, value);
	}

	/// <summary>
	/// Resized width. Bound two-way to the target's width.
	/// </summary>
	public double Width
	{
		get => GetValue(WidthProperty);
		set => SetValue(WidthProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="Edge" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<WindowEdge> EdgeProperty = AvaloniaProperty
		.Register<DragResizeBehavior, WindowEdge>(name: nameof(Edge));

	/// <summary>
	/// Identifies the <see cref="Height" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<double> HeightProperty = AvaloniaProperty
		.Register<DragResizeBehavior, double>(name: nameof(Height), defaultBindingMode: BindingMode.TwoWay);

	/// <summary>
	/// Identifies the <see cref="MinimumSize" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<double> MinimumSizeProperty = AvaloniaProperty
		.Register<DragResizeBehavior, double>(name: nameof(MinimumSize), defaultValue: 100.0);

	/// <summary>
	/// Identifies the <see cref="Width" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<double> WidthProperty = AvaloniaProperty
		.Register<DragResizeBehavior, double>(name: nameof(Width), defaultBindingMode: BindingMode.TwoWay);
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="Thumb.DragDeltaEvent" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_DragDelta(object? sender, VectorEventArgs e)
	{
		bool changesWidth = Edge is WindowEdge.West or WindowEdge.East
			or WindowEdge.NorthWest or WindowEdge.NorthEast
			or WindowEdge.SouthWest or WindowEdge.SouthEast;

		bool changesHeight = Edge is WindowEdge.North or WindowEdge.South
			or WindowEdge.NorthWest or WindowEdge.NorthEast
			or WindowEdge.SouthWest or WindowEdge.SouthEast;

		// Left / top edges shrink with a positive delta, right / bottom edges grow with it.
		double newWidth = Width + (Edge is WindowEdge.West or WindowEdge.NorthWest or WindowEdge.SouthWest
			? -e.Vector.X
			: e.Vector.X);

		double newHeight = Height + (Edge is WindowEdge.North or WindowEdge.NorthWest or WindowEdge.NorthEast
			? -e.Vector.Y
			: e.Vector.Y);

		// Ignore the step if any affected axis would drop to the minimum (matches the previous command logic).
		if ((changesWidth && newWidth <= MinimumSize) || (changesHeight && newHeight <= MinimumSize))
		{
			return;
		}

		if (changesWidth)
		{
			Width = newWidth;
		}

		if (changesHeight)
		{
			Height = newHeight;
		}
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		AssociatedObject?.AddHandler(Thumb.DragDeltaEvent, AssociatedObject_DragDelta);
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		AssociatedObject?.RemoveHandler(Thumb.DragDeltaEvent, AssociatedObject_DragDelta);
	}
	#endregion
}
