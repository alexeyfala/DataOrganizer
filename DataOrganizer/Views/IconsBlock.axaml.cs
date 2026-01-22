using Avalonia;
using Avalonia.Controls;

namespace DataOrganizer.Views;

internal sealed partial class IconsBlock : UserControl
{
	#region Properties
	/// <summary>
	/// A tooltip for hotkeys.
	/// </summary>
	public string? HotkeysToolTip
	{
		get => GetValue(HotkeysToolTipProperty);
		set => SetValue(HotkeysToolTipProperty, value);
	}

	/// <summary>
	/// Returns <c>True</c> if the file is opened in editor.
	/// </summary>
	public bool IsEdited
	{
		get => GetValue(IsEditedProperty);
		set => SetValue(IsEditedProperty, value);
	}

	/// <summary>
	/// Returns <c>True</c> if the file is executed in the operating system.
	/// </summary>
	public bool IsExecuted
	{
		get => GetValue(IsExecutedProperty);
		set => SetValue(IsExecutedProperty, value);
	}

	/// <summary>
	/// Used in "Favorites" mode.
	/// </summary>
	public bool IsFavorite
	{
		get => GetValue(IsFavoriteProperty);
		set => SetValue(IsFavoriteProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="HotkeysToolTip" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> HotkeysToolTipProperty = AvaloniaProperty
		.Register<IconsBlock, string?>(name: nameof(HotkeysToolTip));

	/// <summary>
	/// Identifies the <see cref="IsEdited" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsEditedProperty = AvaloniaProperty
		.Register<IconsBlock, bool>(name: nameof(IsEdited));

	/// <summary>
	/// Identifies the <see cref="IsExecuted" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsExecutedProperty = AvaloniaProperty
		.Register<IconsBlock, bool>(name: nameof(IsExecuted));

	/// <summary>
	/// Identifies the <see cref="IsFavorite" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsFavoriteProperty = AvaloniaProperty
		.Register<IconsBlock, bool>(name: nameof(IsFavorite));
	#endregion

	#region Constructors
	public IconsBlock()
	{
		InitializeComponent();
	}
	#endregion
}