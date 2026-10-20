using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Threading;
using System;

namespace DataOrganizer.Controls.Text;

/// <summary>
/// Automation peer of a <see cref="TextEditorBase" />: an edit control whose value is its text, unless the text is sensitive.
/// </summary>
internal sealed class TextEditorAutomationPeer : ControlAutomationPeer, IValueProvider
{
	#region Properties
	/// <inheritdoc />
	public bool IsReadOnly => Editor.IsReadOnly;

	/// <inheritdoc />
	public string? Value => Editor.Document?.Text;

	/// <summary>
	/// Editor of the peer.
	/// </summary>
	private TextEditorBase Editor => (TextEditorBase)Owner;
	#endregion

	#region Constructors
	public TextEditorAutomationPeer(TextEditorBase owner)
		: base(owner)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void SetValue(string? value)
	{
		if (IsReadOnly || Editor.Document is not { } document)
		{
			throw new InvalidOperationException("The text of the editor cannot be changed.");
		}

		document.Text = value ?? string.Empty;
	}

	/// <inheritdoc />
	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;

	/// <inheritdoc />
	protected override object? GetProviderCore(Type providerType)
	{
		if (providerType != typeof(IValueProvider))
		{
			return base.GetProviderCore(providerType);
		}

		// The bridge of the platform asks for patterns on a thread of its own, and the editor answers on the UI thread.
		bool isSensitive = Dispatcher
			.UIThread
			.Invoke(() => Editor.IsSensitive);

		// Any process of the user can read a value, so a sensitive text has none.
		return isSensitive
			? null
			: base.GetProviderCore(providerType);
	}

	/// <inheritdoc />
	protected override bool HasKeyboardFocusCore() => Editor.TextArea.IsFocused;

	/// <inheritdoc />
	protected override bool IsKeyboardFocusableCore() => Editor.TextArea.Focusable;

	/// <inheritdoc />
	protected override void SetFocusCore()
	{
		// The text area takes the keys, not the editor around it.
		_ = Editor.TextArea.Focus();
	}
	#endregion
}
