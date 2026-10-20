using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Controls;
using Material.Icons;

namespace DataOrganizer.UnitTests.Controls;

[TestFixture(Description = $@"Tests of ""{nameof(FlyoutButtonAutomationPeer)}"" type")]
internal class FlyoutButtonAutomationPeerTests
{
	#region Methods
	/// <summary>
	/// <see cref="AutomationPeer.GetName" />: a button with a header is named by the header, not by its tip.
	/// </summary>
	[AvaloniaTest]
	public void GetName_Gives_The_Header()
	{
		// Arrange
		const string header = "Rename";

		FlyoutButton button = new()
		{
			Header = header,
			Icon = MaterialIconKind.FormTextbox
		};

		ToolTip.SetTip(button, "Renames the tab");

		Show(button);

		AutomationPeer sut = ControlAutomationPeer.CreatePeerForElement(button);

		// Act
		string name = sut.GetName();

		// Assert
		name
			.Should()
			.Be(header);
	}

	/// <summary>
	/// <see cref="AutomationPeer.GetName" />: a button without a header is named by its tip.
	/// </summary>
	[AvaloniaTest]
	public void GetName_Without_A_Header_Gives_The_Tip()
	{
		// Arrange
		const string tip = "Copy";

		FlyoutButton button = new()
		{
			Icon = MaterialIconKind.ContentCopy
		};

		ToolTip.SetTip(button, tip);

		Show(button);

		AutomationPeer sut = ControlAutomationPeer.CreatePeerForElement(button);

		// Act
		string name = sut.GetName();

		// Assert
		name
			.Should()
			.Be(tip);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Shows the button in a window, so that the styles of the application name it.
	/// </summary>
	private static void Show(FlyoutButton button)
	{
		Window window = new()
		{
			Content = button
		};

		window.Show();
	}
	#endregion
}
