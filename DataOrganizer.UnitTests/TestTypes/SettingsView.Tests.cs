using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Services.Settings;
using DataOrganizer.ViewModels;
using DataOrganizer.Views.Settings;
using DataOrganizer.Wrappers;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(SettingsView)}"" type")]
internal class SettingsViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="SettingsView" />: switching a settings category does not change the height of the view.
	/// </summary>
	[AvaloniaTest]
	public void Keeps_Its_Height_Across_Categories()
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

	/// <summary>
	/// <see cref="SettingsView" />: opens on the category kept for the session and reports the newly selected one.
	/// </summary>
	[AvaloniaTest]
	public void Opens_On_The_Category_Kept_For_The_Session()
	{
		// Arrange
		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		settingsStore
			.Settings
			.Returns(TestUtils.CreateRandomSettings());

		SettingsSessionState sessionState = new() { LastCategoryIndex = 2 };

		SettingsViewModel viewModel = new(
			settingsStore,
			Substitute.For<IAppThemeService>(),
			sessionState);

		SettingsView sut = new(viewModel);

		Window window = new() { Content = sut };

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert
		ListBox categories = sut.GetControl<ListBox>("CategoriesListBox");

		categories.SelectedIndex
			.Should()
			.Be(2);

		// Act
		categories.SelectedIndex = 1;

		Dispatcher.UIThread.RunJobs();

		// Assert
		sessionState.LastCategoryIndex
			.Should()
			.Be(1);
	}
	#endregion
}
