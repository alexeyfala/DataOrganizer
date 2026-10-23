using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Primitives;
using System.Windows.Input;

namespace DataOrganizer.Controls;

/// <summary>
/// The buttons at the bottom of a dialog: the main one, which Enter presses, and the one that cancels, which Esc presses.
/// </summary>
internal sealed class DialogButtons : TemplatedControl
{
	#region Properties
	/// <summary>
	/// Command of the button that cancels the dialog.
	/// </summary>
	public ICommand? CancelCommand
	{
		get => GetValue(CancelCommandProperty);
		set => SetValue(CancelCommandProperty, value);
	}

	/// <summary>
	/// Command of the main button.
	/// </summary>
	public ICommand? Command
	{
		get => GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	/// <summary>
	/// Text of the main button.
	/// </summary>
	public string? Text
	{
		get => GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="CancelCommand" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<ICommand?> CancelCommandProperty = AvaloniaProperty
		.Register<DialogButtons, ICommand?>(name: nameof(CancelCommand));

	/// <summary>
	/// Identifies the <see cref="Command" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<ICommand?> CommandProperty = AvaloniaProperty
		.Register<DialogButtons, ICommand?>(name: nameof(Command));

	/// <summary>
	/// Identifies the <see cref="Text" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty
		.Register<DialogButtons, string?>(name: nameof(Text));
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override AutomationPeer OnCreateAutomationPeer() => new NoneAutomationPeer(this);
	#endregion
}
