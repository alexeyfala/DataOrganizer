using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Headless.NUnit;
using AvaloniaEdit.Document;
using AwesomeAssertions;
using DataOrganizer.Controls.Text;
using System;
using Action = System.Action;

namespace DataOrganizer.UnitTests.Controls.Text;

[TestFixture(Description = $@"Tests of ""{nameof(TextEditorAutomationPeer)}"" type")]
internal class TextEditorAutomationPeerTests
{
	#region Methods
	/// <summary>
	/// <see cref="AutomationPeer.GetAutomationControlType" />: an editor is an edit control.
	/// </summary>
	[AvaloniaTest]
	public void GetAutomationControlType_Is_Edit()
	{
		// Arrange
		AutomationPeer sut = ControlAutomationPeer.CreatePeerForElement(new ConsoleLogViewer());

		// Act
		AutomationControlType type = sut.GetAutomationControlType();

		// Assert
		type
			.Should()
			.Be(AutomationControlType.Edit);
	}

	/// <summary>
	/// <see cref="AutomationPeer.GetProvider{T}" />: the text of a sensitive editor is not given away as a value.
	/// </summary>
	[AvaloniaTest]
	public void GetProvider_Of_A_Sensitive_Editor_Gives_No_Value()
	{
		// Arrange
		ConsoleLogViewer editor = new()
		{
			Document = new TextDocument("secret"),
			IsSensitive = true
		};

		AutomationPeer sut = ControlAutomationPeer.CreatePeerForElement(editor);

		// Act
		IValueProvider? provider = sut.GetProvider<IValueProvider>();

		// Assert
		provider
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="AutomationPeer.SetFocus" />: the focus goes to the text area, and the editor reports it.
	/// </summary>
	[AvaloniaTest]
	public void SetFocus_Focuses_The_Text_Area()
	{
		// Arrange
		ConsoleLogViewer editor = new();

		HeadlessWindow.ShowInFluentTheme(editor);

		AutomationPeer sut = ControlAutomationPeer.CreatePeerForElement(editor);

		// Act
		sut.SetFocus();

		// Assert
		editor.TextArea.IsFocused
			.Should()
			.BeTrue();

		sut.HasKeyboardFocus()
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="IValueProvider.SetValue" />: a read-only editor refuses a new text and keeps its own.
	/// </summary>
	[AvaloniaTest]
	public void SetValue_Of_A_Read_Only_Editor_Throws()
	{
		// Arrange
		const string text = "old";

		ConsoleLogViewer editor = new()
		{
			Document = new TextDocument(text),
			IsReadOnly = true
		};

		IValueProvider sut = ControlAutomationPeer
			.CreatePeerForElement(editor)
			.GetProvider<IValueProvider>()!;

		// Act
		Action act = () => sut.SetValue("new");

		// Assert
		act
			.Should()
			.ThrowExactly<InvalidOperationException>();

		editor.Document.Text
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="IValueProvider.SetValue" />: a new text replaces the whole document.
	/// </summary>
	[AvaloniaTest]
	public void SetValue_Replaces_The_Text()
	{
		// Arrange
		const string text = "new";

		ConsoleLogViewer editor = new()
		{
			Document = new TextDocument("old")
		};

		IValueProvider sut = ControlAutomationPeer
			.CreatePeerForElement(editor)
			.GetProvider<IValueProvider>()!;

		// Act
		sut.SetValue(text);

		// Assert
		editor.Document.Text
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="IValueProvider.Value" />: the value is the text of the document.
	/// </summary>
	[AvaloniaTest]
	public void Value_Gives_The_Text()
	{
		// Arrange
		const string text = "line 1\nline 2";

		ConsoleLogViewer editor = new()
		{
			Document = new TextDocument(text)
		};

		IValueProvider sut = ControlAutomationPeer
			.CreatePeerForElement(editor)
			.GetProvider<IValueProvider>()!;

		// Act
		string? value = sut.Value;

		// Assert
		value
			.Should()
			.Be(text);
	}
	#endregion
}
