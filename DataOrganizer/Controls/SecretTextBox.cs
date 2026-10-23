using Avalonia.Automation.Peers;
using Avalonia.Controls;
using System;

namespace DataOrganizer.Controls;

/// <summary>
/// A text box that gives no value to UI Automation while it masks its text with <see cref="TextBox.PasswordChar" />.
/// </summary>
internal sealed class SecretTextBox : TextBox
{
	#region Properties
	/// <inheritdoc />
	protected override Type StyleKeyOverride { get; } = typeof(TextBox);
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override AutomationPeer OnCreateAutomationPeer() => new SecretTextBoxAutomationPeer(this);
	#endregion
}
