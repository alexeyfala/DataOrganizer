using AppInstance.Interfaces;
using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Services.Views;
using DataOrganizer.UnitTests.Fakes;
using DialogContracts.Constants;
using DialogHostAvalonia;
using NSubstitute;
using Shared.Extensions;

namespace DataOrganizer.UnitTests.Services.Views;

[TestFixture(Description = $@"Tests of ""{nameof(DesktopAccessor)}"" type")]
internal class DesktopAccessorTests
{
	#region Methods
	/// <summary>
	/// <see cref="DesktopAccessor.CloseAllWindows" />: every open window is closed.
	/// </summary>
	[AvaloniaTest]
	public void CloseAllWindows_Closes_Every_Window()
	{
		// Arrange
		Window[] windows = [new(), new()];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns(windows);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();
		});

		DesktopAccessor sut = mock.Create<DesktopAccessor>();

		windows.ForEach(x => x.Show());

		// Act
		sut.CloseAllWindows();

		// Assert
		windows
			.Should()
			.OnlyContain(x => !x.IsVisible);
	}

	/// <summary>
	/// <see cref="DesktopAccessor.FindDialogHost" />: finds the host with the identifier among the hosts of every window.
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

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns([mainWindow, notepadWindow]);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();
		});

		DesktopAccessor sut = mock.Create<DesktopAccessor>();

		// Act
		DialogHost? host = sut.FindDialogHost(DialogHostIdentifiers.Notepad);

		// Assert
		host
			.Should()
			.BeSameAs(notepadHost);
	}

	/// <summary>
	/// <see cref="DesktopAccessor.FindMainWindow" />: finds the main window among the other windows.
	/// </summary>
	[AvaloniaTest]
	public void FindMainWindow_Finds_The_Main_Window_Among_The_Others()
	{
		// Arrange
		TestMainWindow mainWindow = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns([new Window(), mainWindow]);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();
		});

		DesktopAccessor sut = mock.Create<DesktopAccessor>();

		// Act
		Window? window = sut.FindMainWindow();

		// Assert
		window
			.Should()
			.BeSameAs(mainWindow);
	}

	/// <summary>
	/// <see cref="DesktopAccessor.FindWindow{T}" />: no desktop has no windows.
	/// </summary>
	[Test]
	public void FindWindow_Finds_Nothing_Without_A_Desktop_Lifetime()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		DesktopAccessor sut = mock.Create<DesktopAccessor>();

		// Act
		Window? window = sut.FindWindow<Window>();

		// Assert
		window
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DesktopAccessor.FindWindow{T}" />: finds the first window of the type that meets the condition, and the
	/// first window of the type without one.
	/// </summary>
	[AvaloniaTest]
	public void FindWindow_Finds_The_First_Window_Of_The_Type_That_Meets_The_Condition()
	{
		// Arrange
		Window first = new()
		{
			Title = "First"
		};

		Window second = new()
		{
			Title = "Second"
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

			Application app = Substitute.For<Application>();

			lifetime
				.Windows
				.Returns([first, second]);

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();
		});

		DesktopAccessor sut = mock.Create<DesktopAccessor>();

		// Act
		Window? any = sut.FindWindow<Window>();

		Window? matching = sut.FindWindow<Window>(x => x.Title == "Second");

		// Assert
		any
			.Should()
			.BeSameAs(first);

		matching
			.Should()
			.BeSameAs(second);
	}

	/// <summary>
	/// <see cref="DesktopAccessor.Shutdown" />: with no desktop lifetime to shut down, the process is ended.
	/// </summary>
	[Test]
	public void Shutdown_Ends_The_Process_Without_A_Desktop_Lifetime()
	{
		// Arrange
		IProcessTerminator processTerminator = Substitute.For<IProcessTerminator>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(processTerminator));

		DesktopAccessor sut = mock.Create<DesktopAccessor>();

		// Act
		sut.Shutdown();

		// Assert
		processTerminator
			.Received(1)
			.Terminate(Arg.Any<int>());
	}

	/// <summary>
	/// <see cref="DesktopAccessor.Shutdown" />: the desktop application shuts down, and the process is left to it.
	/// </summary>
	[Test]
	public void Shutdown_Shuts_The_Desktop_Down()
	{
		// Arrange
		IClassicDesktopStyleApplicationLifetime lifetime = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

		IProcessTerminator processTerminator = Substitute.For<IProcessTerminator>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			Application app = Substitute.For<Application>();

			app.ApplicationLifetime = lifetime;

			builder.RegisterInstance(app).As<Application>();

			builder.RegisterInstance(processTerminator);
		});

		DesktopAccessor sut = mock.Create<DesktopAccessor>();

		// Act
		sut.Shutdown();

		// Assert
		lifetime
			.Received(1)
			.Shutdown(Arg.Any<int>());

		processTerminator
			.DidNotReceiveWithAnyArgs()
			.Terminate();
	}
	#endregion
}
