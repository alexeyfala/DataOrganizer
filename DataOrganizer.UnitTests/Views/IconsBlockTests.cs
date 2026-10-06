using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Views;
using Material.Icons;
using Material.Icons.Avalonia;
using System.Linq;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(IconsBlock)}"" type")]
internal class IconsBlockTests
{
	#region Methods
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
