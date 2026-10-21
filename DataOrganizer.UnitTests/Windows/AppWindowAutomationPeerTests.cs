using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.Windows;

namespace DataOrganizer.UnitTests.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(AppWindowAutomationPeer)}"" type")]
internal class AppWindowAutomationPeerTests
{
	#region Methods
	/// <summary>
	/// <see cref="AutomationPeer.GetName" />: a window with an automation name is named by it, not by its title.
	/// </summary>
	[AvaloniaTest]
	public void GetName_Gives_The_Automation_Name()
	{
		// Arrange
		const string name = "Clipboard";

		PlainWindow window = new()
		{
			Title = "Window"
		};

		AutomationProperties.SetName(window, name);

		AutomationPeer sut = ControlAutomationPeer.CreatePeerForElement(window);

		// Act
		string result = sut.GetName();

		// Assert
		result
			.Should()
			.Be(name);
	}

	/// <summary>
	/// <see cref="AutomationPeer.GetName" />: a window without an automation name is named by its title.
	/// </summary>
	[AvaloniaTest]
	public void GetName_Without_An_Automation_Name_Gives_The_Title()
	{
		// Arrange
		const string title = "Data Organizer - Notepad";

		PlainWindow window = new()
		{
			Title = title
		};

		AutomationPeer sut = ControlAutomationPeer.CreatePeerForElement(window);

		// Act
		string result = sut.GetName();

		// Assert
		result
			.Should()
			.Be(title);
	}
	#endregion
}
