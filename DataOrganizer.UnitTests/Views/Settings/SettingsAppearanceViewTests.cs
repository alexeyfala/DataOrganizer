using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Enums.Settings;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services.Settings;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels.Dialogs;
using DataOrganizer.Views.Settings;
using NSubstitute;
using System.Linq;

namespace DataOrganizer.UnitTests.Views.Settings;

[TestFixture(Description = $@"Tests of ""{nameof(SettingsAppearanceView)}"" type")]
internal class SettingsAppearanceViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="SettingsAppearanceView" />: the radio buttons of the theme, system, light and dark, show the theme of the
	/// settings, and a click on one of them chooses its theme.
	/// </summary>
	[AvaloniaTest]
	public void Theme_Buttons_Show_The_Theme_And_Choose_Another()
	{
		// Arrange
		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		settingsStore
			.Settings
			.Returns(SettingsFactory.CreateSettings());

		SettingsViewModel viewModel = new(
			Substitute.For<IDialogHostCloser>(),
			new SettingsSession(
				settingsStore,
				Substitute.For<IAppThemeService>(),
				Substitute.For<ISettingsApplier>()),
			new SettingsSessionState());

		SettingsAppearanceView sut = new()
		{
			DataContext = viewModel
		};

		Window window = new()
		{
			Content = sut
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		RadioButton[] buttons = [.. sut.GetVisualDescendants().OfType<RadioButton>().Take(3)];

		bool?[] shown = [.. buttons.Select(static x => x.IsChecked)];

		// Act
		buttons[1].IsChecked = true;

		Dispatcher.UIThread.RunJobs();

		// Assert
		shown
			.Should()
			.Equal(false, false, true);

		viewModel.Theme
			.Should()
			.Be(ThemeMode.Light);
	}
	#endregion
}
