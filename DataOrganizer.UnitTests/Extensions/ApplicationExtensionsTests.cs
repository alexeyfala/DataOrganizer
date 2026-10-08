using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.ViewModels.Windows;
using DialogHostAvalonia;
using NSubstitute;

namespace DataOrganizer.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(ApplicationExtensions)}"" type")]
internal class ApplicationExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="ApplicationExtensions.FindDialogHost" />: finds the host with the identifier among the hosts of every
	/// window.
	/// </summary>
	[AvaloniaTest]
	public void FindDialogHost_Finds_The_Host_With_The_Identifier()
	{
		// Arrange
		DialogHost notepadHost = new()
		{
			Identifier = DialogHostIdentifiers.Notepad
		};

		Window mainWindow = new()
		{
			Content = new DialogHost
			{
				Identifier = DialogHostIdentifiers.Main
			}
		};

		Window notepadWindow = new()
		{
			Content = notepadHost
		};

		IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

		lifetime
			.Windows
			.Returns([mainWindow, notepadWindow]);

		Application app = Substitute.For<Application>();

		app.ApplicationLifetime = lifetime;

		// Act
		DialogHost? host = app.FindDialogHost(DialogHostIdentifiers.Notepad);

		// Assert
		host
			.Should()
			.BeSameAs(notepadHost);
	}

	/// <summary>
	/// <see cref="ApplicationExtensions.FindMainWindow" />: finds the window of a main view model among the other windows.
	/// </summary>
	[AvaloniaTest]
	public void FindMainWindow_Finds_The_Window_Of_A_Main_View_Model()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		Window mainWindow = new()
		{
			DataContext = mock.Create<FavoritesViewModel>()
		};

		IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

		lifetime
			.Windows
			.Returns([new Window(), mainWindow]);

		Application app = Substitute.For<Application>();

		app.ApplicationLifetime = lifetime;

		// Act
		Window? window = app.FindMainWindow();

		// Assert
		window
			.Should()
			.BeSameAs(mainWindow);
	}
	#endregion
}
