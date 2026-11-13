using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Controls;
using System.Linq;

namespace DataOrganizer.UnitTests.Controls;

[TestFixture(Description = $@"Tests of ""{nameof(DialogButtons)}"" type")]
internal class DialogButtonsTests
{
	#region Methods
	/// <summary>
	/// <see cref="DialogButtons" />: the bar itself is no element for UI Automation, so its buttons stand in the dialog as
	/// before.
	/// </summary>
	[AvaloniaTest]
	public void OnCreateAutomationPeer_Keeps_The_Bar_Out_Of_UI_Automation()
	{
		// Arrange
		DialogButtons sut = new();

		// Act
		AutomationPeer peer = ControlAutomationPeer.CreatePeerForElement(sut);

		// Assert
		peer.IsControlElement()
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DialogButtons" />: the first button runs <see cref="DialogButtons.Command" />, shows
	/// <see cref="DialogButtons.Text" /> and takes Enter; the second runs <see cref="DialogButtons.CancelCommand" /> and takes
	/// Esc.
	/// </summary>
	[AvaloniaTest]
	public void Template_Gives_Each_Button_Its_Command_And_Key()
	{
		// Arrange
		RelayCommand command = new(static () => { });

		RelayCommand cancelCommand = new(static () => { });

		DialogButtons sut = new()
		{
			CancelCommand = cancelCommand,
			Command = command,
			Text = "Apply"
		};

		Window window = new()
		{
			Content = sut
		};

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert
		Button[] buttons = [.. sut.GetVisualDescendants().OfType<Button>()];

		buttons
			.Select(static x => (x.Command, x.IsDefault, x.IsCancel))
			.Should()
			.Equal((command, true, false), (cancelCommand, false, true));

		buttons[0].Content
			.Should()
			.Be("Apply");
	}
	#endregion
}
