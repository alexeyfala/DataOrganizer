using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using Material.Styles.Assists;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards that a text box is named for UI Automation by its label, or else by its placeholder")]
internal class TextBoxAutomationNameTests
{
	#region Methods
	/// <summary>
	/// A text box with a Material label is named by the label.
	/// </summary>
	[AvaloniaTest]
	public void Text_Box_With_A_Label_Is_Named_By_It()
	{
		// Arrange
		const string label = "Password";

		TextBox textBox = new()
		{
			PlaceholderText = "Type here"
		};

		TextFieldAssist.SetLabel(textBox, label);

		Show(textBox);

		// Act
		string name = ControlAutomationPeer
			.CreatePeerForElement(textBox)
			.GetName();

		// Assert
		name
			.Should()
			.Be(label);
	}

	/// <summary>
	/// A text box without a label is named by its placeholder.
	/// </summary>
	[AvaloniaTest]
	public void Text_Box_Without_A_Label_Is_Named_By_Its_Placeholder()
	{
		// Arrange
		const string placeholder = "Search";

		TextBox textBox = new()
		{
			PlaceholderText = placeholder
		};

		Show(textBox);

		// Act
		string name = ControlAutomationPeer
			.CreatePeerForElement(textBox)
			.GetName();

		// Assert
		name
			.Should()
			.Be(placeholder);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Shows the text box in a window, so that the styles of the application name it.
	/// </summary>
	private static void Show(TextBox textBox)
	{
		Window window = new()
		{
			Content = textBox
		};

		window.Show();
	}
	#endregion
}
