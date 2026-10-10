using Avalonia;
using Avalonia.Controls;

namespace DataOrganizer.Views;

/// <summary>
/// Title bar that a window draws itself in its extended client area.
/// </summary>
internal sealed partial class TitleBarView : UserControl
{
	#region Properties
	/// <summary>
	/// Content placed before the switch that keeps the window on top.
	/// </summary>
	public object? ButtonContent
	{
		get => GetValue(ButtonContentProperty);
		set => SetValue(ButtonContentProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="ButtonContent" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<object?> ButtonContentProperty = AvaloniaProperty
		.Register<TitleBarView, object?>(name: nameof(ButtonContent));
	#endregion

	#region Constructors
	public TitleBarView() => InitializeComponent();
	#endregion
}
