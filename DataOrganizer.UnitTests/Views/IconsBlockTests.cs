using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Views;
using Hierarchy.Enums;
using Material.Icons;
using Material.Icons.Avalonia;
using System.Linq;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(IconsBlock)}"" type")]
internal class IconsBlockTests
{
	#region Methods
	/// <summary>
	/// <see cref="IconsBlock.EncryptionStatus" />: the lock shows to UI Automation as an image named by its tip, so the
	/// encryption of an object can be read without a picture of the screen.
	/// </summary>
	[AvaloniaTest]
	public void EncryptionStatus_Shows_As_An_Image_Named_By_Its_Tip(
		[Values(EncryptionStatus.Decrypted, EncryptionStatus.Encrypted)] EncryptionStatus status)
	{
		// Arrange
		IconsBlock sut = new()
		{
			EncryptionStatus = status
		};

		Window window = new()
		{
			Content = sut
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		MaterialIcon icon = sut
			.GetVisualDescendants()
			.OfType<MaterialIcon>()
			.Single(static x => x.IsEffectivelyVisible);

		// Act
		AutomationPeer peer = ControlAutomationPeer.CreatePeerForElement(icon);

		// Assert
		peer.GetAutomationControlType()
			.Should()
			.Be(AutomationControlType.Image);

		peer.GetName()
			.Should()
			.NotBeNullOrEmpty()
			.And
			.Be((string?)ToolTip.GetTip(icon));
	}

	/// <summary>
	/// <see cref="IconsBlock.HotkeysToolTip" />: the keyboard icon keeps its tip while the block is out of the tree.
	/// </summary>
	[AvaloniaTest]
	public void HotkeysToolTip_Stays_On_The_Icon_While_The_Block_Is_Out_Of_The_Tree()
	{
		// Arrange
		const string toolTip = "Ctrl+1";

		IconsBlock sut = new()
		{
			HotkeysToolTip = toolTip
		};

		Window window = new()
		{
			Content = sut
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		MaterialIcon keyboard = sut
			.GetVisualDescendants()
			.OfType<MaterialIcon>()
			.Single(static x => x.Kind == MaterialIconKind.KeyboardVariant);

		// Act
		window.Content = null;

		Dispatcher.UIThread.RunJobs();

		// Assert
		ToolTip.GetTip(keyboard)
			.Should()
			.Be(toolTip);
	}
	#endregion
}
