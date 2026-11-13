using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Styling;
using AwesomeAssertions;
using Material.Icons;
using Material.Icons.Avalonia;
using System;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards that a button showing no text is named for UI Automation by its tip")]
internal class ButtonAutomationNameTests
{
	#region Methods
	/// <summary>
	/// A button of any kind that shows an icon is named by its tip.
	/// </summary>
	[AvaloniaTest]
	[TestCase(typeof(Button))]
	[TestCase(typeof(ToggleButton))]
	public void Icon_Button_Is_Named_By_Its_Tip(Type buttonType)
	{
		// Arrange
		const string tip = "Add";

		Button button = (Button)Activator.CreateInstance(buttonType)!;

		button.Content = new MaterialIcon
		{
			Kind = MaterialIconKind.Plus
		};

		ToolTip.SetTip(button, tip);

		Window window = new()
		{
			Content = button
		};

		window.Show();

		// Act
		string name = ControlAutomationPeer
			.CreatePeerForElement(button)
			.GetName();

		// Assert
		name
			.Should()
			.Be(tip);
	}

	/// <summary>
	/// A button that shows text is named by the text, not by its tip.
	/// </summary>
	[AvaloniaTest]
	public void Text_Button_Is_Named_By_Its_Text()
	{
		// Arrange
		const string text = "Save";

		Button button = new()
		{
			Content = text
		};

		ToolTip.SetTip(button, "Saves the file");

		Window window = new()
		{
			Content = button
		};

		window.Show();

		// Act
		string name = ControlAutomationPeer
			.CreatePeerForElement(button)
			.GetName();

		// Assert
		name
			.Should()
			.Be(text);
	}

	/// <summary>
	/// A text tip that the theme of a button sets names an icon button.
	/// </summary>
	[AvaloniaTest]
	public void Theme_Tip_Names_An_Icon_Button()
	{
		// Arrange
		const string tip = "Clipboard";

		Button button = new()
		{
			Content = new MaterialIcon
			{
				Kind = MaterialIconKind.ClipboardOutline
			},
			Theme = new ControlTheme(typeof(Button))
			{
				Setters =
				{
					new Setter(ToolTip.TipProperty, tip)
				}
			}
		};

		Window window = new()
		{
			Content = button
		};

		window.Show();

		// Act
		string name = ControlAutomationPeer
			.CreatePeerForElement(button)
			.GetName();

		// Assert
		name
			.Should()
			.Be(tip);
	}
	#endregion
}
