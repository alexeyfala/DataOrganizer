using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AwesomeAssertions;
using Hierarchy.UnitTests.Factories;
using System;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards that a list item or a tab of an object with a name is named by it for UI Automation")]
internal class ItemAutomationNameTests
{
	#region Methods
	/// <summary>
	/// The item of a named object in a list or in tabs is named by the object, not by its type.
	/// </summary>
	[AvaloniaTest]
	[TestCase(typeof(ListBox))]
	[TestCase(typeof(TabControl))]
	public void Item_Of_A_Named_Object_Is_Named_By_It(Type controlType)
	{
		// Arrange
		const string name = "main.c";

		ItemsControl items = (ItemsControl)Activator.CreateInstance(controlType)!;

		items.ItemsSource = new[]
		{
			ItemDtoFactory.CreateNamedFileDto(name)
		};

		Window window = new()
		{
			Content = items
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		string itemName = ControlAutomationPeer
			.CreatePeerForElement(items.ContainerFromIndex(0)!)
			.GetName();

		// Assert
		itemName
			.Should()
			.Be(name);
	}
	#endregion
}
