using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Storage;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services.Settings;
using DataOrganizer.Services.Views;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Windows;
using NSubstitute;
using Shared.Interfaces;

namespace DataOrganizer.UnitTests.Services.Views;

[TestFixture(Description = $@"Tests of ""{nameof(ConsoleWindowHost)}"" type")]
internal class ConsoleWindowHostTests
{
	#region Methods
	/// <summary>
	/// <see cref="ConsoleWindowHost.ConfigureAndShowAsync" />: the saved placement of the window and the settings of the
	/// console are applied.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureAndShowAsync_Applies_Saved_Settings()
	{
		// Arrange
		ConsoleViewModel viewModel = new(
			Substitute.For<IAppEnvironment>(),
			Substitute.For<IDirectoryAccessor>(),
			new InlineDispatcherAccessor());

		ConsoleWindow consoleWindow = new(viewModel);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ConsoleWindowSettings settings = new()
			{
				FontSize = 17.0,
				IsTopmost = true,
				Size = new(640, 480),
				WindowState = WindowState.Normal,
				WordWrap = true,
				X = 120,
				Y = 90
			};

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<ConsoleWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<ConsoleViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<ConsoleWindow>(Arg.Any<object[]>())
				.Returns(consoleWindow);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ConsoleWindowHost sut = mock.Create<ConsoleWindowHost>();

		// Act
		_ = sut.ConfigureAndShowAsync();

		// Assert
		consoleWindow.Width
			.Should()
			.Be(640);

		consoleWindow.Height
			.Should()
			.Be(480);

		consoleWindow.Position
			.Should()
			.Be(new PixelPoint(120, 90));

		consoleWindow.Topmost
			.Should()
			.BeTrue();

		viewModel.FontSize
			.Should()
			.Be(17.0);

		viewModel.WordWrap
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ConsoleWindowHost.ConfigureAndShowAsync" />: a window saved off every screen opens in the center of the
	/// screen.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureAndShowAsync_Centers_A_Window_Saved_Off_The_Screens()
	{
		// Arrange
		ConsoleViewModel viewModel = new(
			Substitute.For<IAppEnvironment>(),
			Substitute.For<IDirectoryAccessor>(),
			new InlineDispatcherAccessor());

		ConsoleWindow consoleWindow = new(viewModel);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ConsoleWindowSettings settings = new()
			{
				FontSize = 14.0,
				IsTopmost = false,
				Size = new(640, 480),
				WindowState = WindowState.Normal,
				WordWrap = false,
				X = 50000,
				Y = 50000
			};

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<ConsoleWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<ConsoleViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<ConsoleWindow>(Arg.Any<object[]>())
				.Returns(consoleWindow);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ConsoleWindowHost sut = mock.Create<ConsoleWindowHost>();

		// Act
		_ = sut.ConfigureAndShowAsync();

		// Assert
		consoleWindow.WindowStartupLocation
			.Should()
			.Be(WindowStartupLocation.CenterScreen);
	}

	/// <summary>
	/// <see cref="ConsoleWindowHost.ConfigureAndShowAsync" />: a window closed minimized opens in the normal state.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureAndShowAsync_Opens_A_Window_Saved_Minimized_In_The_Normal_State()
	{
		// Arrange
		ConsoleViewModel viewModel = new(
			Substitute.For<IAppEnvironment>(),
			Substitute.For<IDirectoryAccessor>(),
			new InlineDispatcherAccessor());

		ConsoleWindow consoleWindow = new(viewModel);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ConsoleWindowSettings settings = new()
			{
				FontSize = 14.0,
				IsTopmost = false,
				Size = new(640, 480),
				WindowState = WindowState.Minimized,
				WordWrap = false,
				X = 120,
				Y = 90
			};

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.DeserializeFromFile<ConsoleWindowSettings>(Arg.Any<string>())
				.Returns(settings);

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<ConsoleViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<ConsoleWindow>(Arg.Any<object[]>())
				.Returns(consoleWindow);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ConsoleWindowHost sut = mock.Create<ConsoleWindowHost>();

		// Act
		_ = sut.ConfigureAndShowAsync();

		// Assert
		consoleWindow.WindowState
			.Should()
			.Be(WindowState.Normal);
	}

	/// <summary>
	/// <see cref="ConsoleWindowHost.ConfigureAndShowAsync" />: without saved settings the window takes the default size and
	/// opens in the center of the screen.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureAndShowAsync_Opens_Centered_With_The_Default_Size_For_The_First_Launch()
	{
		// Arrange
		ConsoleViewModel viewModel = new(
			Substitute.For<IAppEnvironment>(),
			Substitute.For<IDirectoryAccessor>(),
			new InlineDispatcherAccessor());

		ConsoleWindow consoleWindow = new(viewModel);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<ConsoleViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<ConsoleWindow>(Arg.Any<object[]>())
				.Returns(consoleWindow);

			builder.RegisterInstance(viewFactory);
		});

		ConsoleWindowHost sut = mock.Create<ConsoleWindowHost>();

		// Act
		_ = sut.ConfigureAndShowAsync();

		// Assert
		consoleWindow.Width
			.Should()
			.Be(IViewLauncher.DefaultWindowSize.Width);

		consoleWindow.Height
			.Should()
			.Be(IViewLauncher.DefaultWindowSize.Height);

		consoleWindow.WindowStartupLocation
			.Should()
			.Be(WindowStartupLocation.CenterScreen);
	}

	/// <summary>
	/// <see cref="ConsoleWindowHost.ConfigureAndShowAsync" />: closing the window saves its placement and the settings of the
	/// console.
	/// </summary>
	[AvaloniaTest]
	public void ConfigureAndShowAsync_Saves_The_Settings_When_The_Window_Closes()
	{
		// Arrange
		ConsoleViewModel viewModel = new(
			Substitute.For<IAppEnvironment>(),
			Substitute.For<IDirectoryAccessor>(),
			new InlineDispatcherAccessor());

		ConsoleWindow consoleWindow = new(viewModel);

		ConsoleWindowSettings? captured = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.When(x => x.SerializeToJsonFile(
					Arg.Any<ConsoleWindowSettings>(),
					Arg.Any<string>(),
					Arg.Any<bool>()))
				.Do(call => captured = call.Arg<ConsoleWindowSettings>());

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<ConsoleViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateWindow<ConsoleWindow>(Arg.Any<object[]>())
				.Returns(consoleWindow);

			builder.RegisterInstance(fileSystem);

			builder.RegisterInstance(viewFactory);

			builder
				.RegisterType<WindowSettingsStore>()
				.As<IWindowSettingsStore>();
		});

		ConsoleWindowHost sut = mock.Create<ConsoleWindowHost>();

		_ = sut.ConfigureAndShowAsync();

		viewModel.FontSize = 18.0;

		viewModel.WordWrap = true;

		consoleWindow.Topmost = true;

		PixelPoint position = consoleWindow.Position;

		// Act
		consoleWindow.Close();

		// Assert
		captured
			.Should()
			.BeEquivalentTo(new ConsoleWindowSettings
			{
				FontSize = 18.0,
				IsTopmost = true,
				Size = new(IViewLauncher.DefaultWindowSize.Width, IViewLauncher.DefaultWindowSize.Height),
				WindowState = WindowState.Normal,
				WordWrap = true,
				X = position.X,
				Y = position.Y
			});
	}

	/// <summary>
	/// <see cref="ConsoleWindowHost.Write" />: the text reaches the log of the console before its window opens.
	/// </summary>
	[AvaloniaTest]
	public void Write_Appends_The_Text_To_The_Console_Log()
	{
		// Arrange
		ConsoleViewModel viewModel = new(
			Substitute.For<IAppEnvironment>(),
			Substitute.For<IDirectoryAccessor>(),
			new InlineDispatcherAccessor());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<ConsoleViewModel>()
				.Returns(viewModel);

			builder.RegisterInstance(viewFactory);
		});

		ConsoleWindowHost sut = mock.Create<ConsoleWindowHost>();

		// Act
		sut.Write("record");

		// Assert
		viewModel.Document.Text
			.Should()
			.Be("record");
	}
	#endregion
}
