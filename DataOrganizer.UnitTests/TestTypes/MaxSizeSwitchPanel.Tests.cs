using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Views.Settings;
using DataOrganizer.Wrappers;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(MaxSizeSwitchPanel)}"" type")]
internal class MaxSizeSwitchPanelTests
{
	#region Methods
	/// <summary>
	/// <see cref="MaxSizeSwitchPanel.SelectedIndex" />: only the selected child is enabled and placed inside the panel.
	/// </summary>
	[AvaloniaTest]
	public void Only_The_Selected_Child_Is_Displayed()
	{
		// Arrange
		MaxSizeSwitchPanel sut = new()
		{
			Children =
			{
				new Border { Width = 100.0, Height = 50.0 },
				new Border { Width = 200.0, Height = 150.0 }
			}
		};

		Window window = new() { Content = sut };

		window.Show();

		// Act
		sut.SelectedIndex = 1;

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.Children[0].IsEnabled.Should().BeFalse();

		sut.Children[0].Bounds.Right.Should().BeLessThanOrEqualTo(0.0);

		sut.Children[1].IsEnabled.Should().BeTrue();

		sut.Children[1].Bounds.Left.Should().BeGreaterThanOrEqualTo(0.0);
	}

	/// <summary>
	/// <see cref="MaxSizeSwitchPanel" />: the panel is sized to the largest child regardless of the selection.
	/// </summary>
	[AvaloniaTest]
	public void Panel_Is_Sized_To_The_Largest_Child()
	{
		// Arrange
		MaxSizeSwitchPanel sut = new()
		{
			Children =
			{
				new Border { Width = 100.0, Height = 50.0 },
				new Border { Width = 200.0, Height = 150.0 }
			}
		};

		Window window = new() { Content = sut };

		window.Show();

		Dispatcher.UIThread.RunJobs();

		Size expectedSize = new(200.0, 150.0);

		// Assert
		sut.DesiredSize.Should().Be(expectedSize);

		// Act
		sut.SelectedIndex = 1;

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.DesiredSize.Should().Be(expectedSize);
	}

	/// <summary>
	/// <see cref="SettingsView" />: switching a settings category does not change the height of the view.
	/// </summary>
	[AvaloniaTest]
	public void Settings_View_Keeps_Its_Height_Across_Categories()
	{
		// Arrange
		SettingsView sut = new();

		Window window = new() { Content = sut };

		window.Show();

		Dispatcher.UIThread.RunJobs();

		ListBox categories = sut.GetControl<ListBox>("CategoriesListBox");

		MaxSizeSwitchPanel panel = sut
			.GetVisualDescendants()
			.OfType<MaxSizeSwitchPanel>()
			.Single();

		List<double> heights = [];

		// Act
		for (int index = 0; index < categories.ItemCount; index++)
		{
			categories.SelectedIndex = index;

			Dispatcher.UIThread.RunJobs();

			heights.Add(panel.Bounds.Height);
		}

		// Assert
		heights
			.Should()
			.HaveCount(categories.ItemCount)
			.And
			.OnlyContain(static height => height > 0.0);

		heights
			.Distinct()
			.Should()
			.HaveCount(1);
	}
	#endregion
}
