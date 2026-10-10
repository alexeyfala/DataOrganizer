using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Controls;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Services.Settings;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels.Dialogs;
using DataOrganizer.Views.Dialogs;
using DataOrganizer.Views.Settings;
using DialogHostAvalonia;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Views.Settings;

[TestFixture(Description = $@"Tests of ""{nameof(SettingsView)}"" type")]
internal class SettingsViewTests
{
	#region Data
	/// <summary>
	/// Name of the category list in the markup.
	/// </summary>
	private const string CategoriesListName = "CategoriesList";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DialogViewBase" />: the Escape key closes the settings in the host of the main window, while the
	/// notepad has a host of its own.
	/// </summary>
	[AvaloniaTest]
	public void Escape_Closes_The_Dialog_Of_Its_Own_Host()
	{
		// Arrange
		Window mainWindow = new()
		{
			Content = new DialogHost
			{
				Identifier = DialogHostIdentifiers.Main
			}
		};

		Window notepadWindow = new()
		{
			Content = new DialogHost
			{
				Identifier = DialogHostIdentifiers.Notepad
			}
		};

		notepadWindow.Show();

		// Shown last, the main window has the keyboard, as when the settings open in it.
		mainWindow.Show();

		Task dialogClosed = DialogHost.Show(new SettingsView(), DialogHostIdentifiers.Main);

		Dispatcher.UIThread.RunJobs();

		// Act
		mainWindow.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		mainWindow.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		Dispatcher.UIThread.RunJobs();

		bool isClosed = dialogClosed.IsCompleted;

		// Closed before the assertion: a closed window takes its host out of the list every headless test shares.
		mainWindow.Close();

		notepadWindow.Close();

		// Assert
		isClosed
			.Should()
			.BeTrue();
	}

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

		ListBox categories = sut.GetControl<ListBox>(CategoriesListName);

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
			.Returns(SettingsFactory.CreateSettings());

		SettingsSessionState sessionState = new() { LastCategoryIndex = 2 };

		SettingsViewModel viewModel = new(
			settingsStore,
			Substitute.For<IAppThemeService>(),
			Substitute.For<IDialogHostCloser>(),
			sessionState);

		SettingsView sut = new(viewModel);

		Window window = new() { Content = sut };

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert
		ListBox categories = sut.GetControl<ListBox>(CategoriesListName);

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
