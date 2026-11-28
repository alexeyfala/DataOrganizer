using Avalonia.Automation.Peers;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.UI.ViewModels.Windows;
using DataOrganizer.UI.Windows;

namespace DataOrganizer.UI.UnitTests.Windows;

/// <summary>
/// Tests of <see cref="ToastWindow" /> type.
/// </summary>
[TestFixture]
internal sealed class ToastWindowTests
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
