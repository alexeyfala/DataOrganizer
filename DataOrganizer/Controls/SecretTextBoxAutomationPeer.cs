using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace DataOrganizer.Controls;

/// <summary>
/// Automation peer of a <see cref="SecretTextBox" />: its value is empty while the text is masked, and a new value is
/// still written.
/// </summary>
internal sealed class SecretTextBoxAutomationPeer : TextBoxAutomationPeer, IValueProvider
{
	#region Properties
	/// <summary>
	/// The text of the box, or an empty string while it is masked, the eye open or closed.
	/// </summary>
	public new string? Value => IsMasked ? string.Empty : Owner.Text;

	/// <summary>
	/// <c>True</c> while the box masks its text. Read on the UI thread only: the bridge of the platform reads a value there,
	/// but asks for the patterns on a thread of its own.
	/// </summary>
	private bool IsMasked => Owner.PasswordChar != default;
	#endregion

	#region Constructors
	public SecretTextBoxAutomationPeer(SecretTextBox owner)
		: base(owner)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		// The change of a masked text would carry the text itself.
		if (e.Property == TextBox.TextProperty && IsMasked)
		{
			return;
		}

		base.OwnerPropertyChanged(sender, e);
	}
	#endregion
}
