using Avalonia.Automation.Peers;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Windows;

namespace DataOrganizer.UnitTests.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(ToastWindow)}"" type")]
internal class ToastWindowTests
{
	#region Methods
	/// <summary>
	/// <see cref="AutomationPeer.GetName" />: a toast is named for UI Automation by its title.
	/// </summary>
	[AvaloniaTest]
	public void GetName_Gives_The_Title_Of_The_Toast()
	{
		// Arrange
		const string title = "Data Organizer";

		ToastWindow window = new(new ToastViewModel
		{
			Message = "Copied",
			Title = title
		});

		window.Show();

		AutomationPeer sut = ControlAutomationPeer.CreatePeerForElement(window);

		// Act
		string name = sut.GetName();

		// Assert
		name
			.Should()
			.Be(title);
	}
	#endregion
}
