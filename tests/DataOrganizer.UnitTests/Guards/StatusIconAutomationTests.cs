using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using Material.Icons;
using Material.Icons.Avalonia;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards that an icon telling the state of an object shows to UI Automation as an image named by its tip")]
internal class StatusIconAutomationTests
{
	#region Data
	/// <summary>
	/// Style class of an icon that tells the state of an object.
	/// </summary>
	private const string StatusIconClass = "StatusIconStyle";
	#endregion

	#region Methods
	/// <summary>
	/// An icon without the class stays out of the tree of controls, as the control it belongs to names itself.
	/// </summary>
	[AvaloniaTest]
	public void Icon_Without_The_Class_Stays_Out_Of_The_Tree()
	{
		// Arrange
		MaterialIcon icon = new()
		{
			Kind = MaterialIconKind.Plus
		};

		Window window = new()
		{
			Content = icon
		};

		window.Show();

		// Act
		bool result = ControlAutomationPeer
			.CreatePeerForElement(icon)
			.IsControlElement();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// An icon with the class is an image in the tree of controls, named by its tip.
	/// </summary>
	[AvaloniaTest]
	public void Status_Icon_Is_An_Image_Named_By_Its_Tip()
	{
		// Arrange
		const string tip = "Encrypted";

		MaterialIcon icon = new()
		{
			Kind = MaterialIconKind.Lock
		};

		icon.Classes.Add(StatusIconClass);

		ToolTip.SetTip(icon, tip);

		Window window = new()
		{
			Content = icon
		};

		window.Show();

		// Act
		AutomationPeer peer = ControlAutomationPeer.CreatePeerForElement(icon);

		// Assert
		peer.IsControlElement()
			.Should()
			.BeTrue();

		peer.GetAutomationControlType()
			.Should()
			.Be(AutomationControlType.Image);

		peer.GetName()
			.Should()
			.Be(tip);
	}
	#endregion
}
