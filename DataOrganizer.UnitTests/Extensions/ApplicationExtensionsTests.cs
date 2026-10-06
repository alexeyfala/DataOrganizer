using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Extensions;
using DataOrganizer.ViewModels.Windows;
using NSubstitute;

namespace DataOrganizer.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(ApplicationExtensions)}"" type")]
internal class ApplicationExtensionsTests
{
	#region Methods
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
