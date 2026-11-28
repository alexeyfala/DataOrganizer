using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Infrastructure.Settings.Enums;
using DataOrganizer.Infrastructure.Settings.Interfaces;
using DataOrganizer.Infrastructure.Settings.Services;
using DataOrganizer.Infrastructure.UnitTests.Settings.TestHelpers.Factories;
using DataOrganizer.UI.ViewModels.Dialogs;
using DataOrganizer.UI.Views.Settings;
using NSubstitute;
using System.Linq;

namespace DataOrganizer.UI.UnitTests.Views.Settings;

/// <summary>
/// Tests of <see cref="SettingsAppearanceView" /> type.
/// </summary>
[TestFixture]
internal sealed class SettingsAppearanceViewTests
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
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			builder.RegisterInstance(settingsStore);

			builder
				.RegisterType<SettingsSession>()
				.As<ISettingsDraft>();

			builder
				.RegisterType<SettingsSessionState>()
				.As<ISettingsSessionState>();
		});

		SettingsViewModel viewModel = mock.Create<SettingsViewModel>();

		SettingsAppearanceView sut = new()
		{
			DataContext = viewModel
		};

		Window window = new()
		{
			Content = sut
		};

		window.Show();

		Dispatcher
			.UIThread
			.RunJobs();

		RadioButton[] buttons = [.. sut.GetVisualDescendants().OfType<RadioButton>().Take(3)];

		bool?[] shown = [.. buttons.Select(static x => x.IsChecked)];

		// Act
		buttons[1].IsChecked = true;

		Dispatcher
			.UIThread
			.RunJobs();

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
