using Avalonia;
using Avalonia.Controls;
using DataOrganizer.Enums;

namespace DataOrganizer.Views;

internal sealed partial class IconsBlock : UserControl
{
	#region Properties
	/// <inheritdoc cref="Enums.EncryptionStatus" />
	public EncryptionStatus EncryptionStatus
	{
		get => GetValue(EncryptionStatusProperty);
		set => SetValue(EncryptionStatusProperty, value);
	}

	/// <summary>
	/// A tooltip for hotkeys.
	/// </summary>
	public string? HotkeysToolTip
	{
		get => GetValue(HotkeysToolTipProperty);
		set => SetValue(HotkeysToolTipProperty, value);
	}

	/// <summary>
	/// <c>True</c> when the file is opened in the built-in editor.
	/// </summary>
	public bool IsEditing
	{
		get => GetValue(IsEditingProperty);
		set => SetValue(IsEditingProperty, value);
	}

	/// <summary>
	/// <c>True</c> when the file is executing in the operating system.
	/// </summary>
	public bool IsExecuting
	{
		get => GetValue(IsExecutingProperty);
		set => SetValue(IsExecutingProperty, value);
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
	/// Identifies the <see cref="EncryptionStatus" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<EncryptionStatus> EncryptionStatusProperty = AvaloniaProperty
		.Register<IconsBlock, EncryptionStatus>(name: nameof(EncryptionStatus));

	/// <summary>
	/// Identifies the <see cref="HotkeysToolTip" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> HotkeysToolTipProperty = AvaloniaProperty
		.Register<IconsBlock, string?>(name: nameof(HotkeysToolTip));

	/// <summary>
	/// Identifies the <see cref="IsEditing" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsEditingProperty = AvaloniaProperty
		.Register<IconsBlock, bool>(name: nameof(IsEditing));

	/// <summary>
	/// Identifies the <see cref="IsExecuting" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsExecutingProperty = AvaloniaProperty
		.Register<IconsBlock, bool>(name: nameof(IsExecuting));

	/// <summary>
	/// Identifies the <see cref="IsFavorite" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsFavoriteProperty = AvaloniaProperty
		.Register<IconsBlock, bool>(name: nameof(IsFavorite));
	#endregion

	#region Constructors
	public IconsBlock() => InitializeComponent();
	#endregion
}
