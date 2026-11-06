using Autofac;
using Autofac.Extras.Moq;
using FileSystemOperations.Interfaces;
using FileSystemOperations.Services;
using NSubstitute;
using Shared.Common;
using Shared.Enums;
using Shared.Interfaces;
using System.IO;
using System.IO.Abstractions;
using Testably.Abstractions.Testing;
using TestSupport.Common;

namespace FileSystemOperations.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(DirectoryAccessor)}"" type")]
internal class DirectoryAccessorTests
{
	#region Data
	/// <summary>
	/// Name of the folder the application is in.
	/// </summary>
	private const string AppFolderName = "Apps";

	/// <summary>
	/// Name of the entry assembly of the application.
	/// </summary>
	private const string AppName = "DataOrganizer";

	/// <summary>
	/// Name of the folder that holds the file.
	/// </summary>
	private const string FolderName = "Docs";

	/// <summary>
	/// Folder the application is in.
	/// </summary>
	private static readonly string AppFolder = TestPath.Combine(AppFolderName);

	/// <summary>
	/// Path of the shared <c>dotnet</c> host.
	/// </summary>
	private static readonly string DotnetHostPath = TestPath.Combine("dotnet", "dotnet");

	/// <summary>
	/// Path of the file to reveal.
	/// </summary>
	private static readonly string FilePath = TestPath.Combine(FolderName, "notes.txt");

	/// <summary>
	/// Path of the folder that holds the file.
	/// </summary>
	private static readonly string FolderPath = TestPath.Combine(FolderName);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DirectoryAccessor.OpenAppDirectory" />: an AppImage is revealed itself, not the file in its temporary
	/// mount that started the process.
	/// </summary>
	[Test]
	public void OpenAppDirectory_In_An_AppImage_Reveals_The_AppImage()
	{
		// Arrange
		string appImagePath = TestPath.Combine("Downloads", "DataOrganizer.AppImage");

		IExplorerManager explorerManager = Substitute.For<IExplorerManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			string mountedAppPath = TestPath.Combine("tmp", ".mount_DataOr", AppName);

			IAppProcess appProcess = Substitute.For<IAppProcess>();

			MockFileSystem fileSystem = new();

			IPlatform platform = Substitute.For<IPlatform>();

			appProcess
				.GetEnvironmentVariable("APPIMAGE")
				.Returns(appImagePath);

			appProcess
				.ProcessPath
				.Returns(mountedAppPath);

			fileSystem
				.Directory
				.CreateDirectory(Path.GetDirectoryName(appImagePath)!);

			fileSystem
				.File
				.WriteAllBytes(appImagePath, []);

			fileSystem
				.Directory
				.CreateDirectory(Path.GetDirectoryName(mountedAppPath)!);

			fileSystem
				.File
				.WriteAllBytes(mountedAppPath, []);

			platform
				.CurrentOS
				.Returns(OperatingSystemKind.Linux);

			platform
				.IsLinux
				.Returns(true);

			// No real file manager may start from a test.
			explorerManager
				.TryForegroundFolder(Arg.Any<string>(), Arg.Any<string?>())
				.Returns(true);

			builder.RegisterInstance(appProcess);

			builder.RegisterInstance(explorerManager);

			builder.RegisterInstance<IFileSystem>(fileSystem);

			builder.RegisterInstance(platform);
		});

		DirectoryAccessor sut = mock.Create<DirectoryAccessor>();

		// Act
		sut.OpenAppDirectory();

		// Assert
		explorerManager
			.Received(1)
			.TryForegroundFolder(Path.GetDirectoryName(appImagePath)!, appImagePath);
	}

	/// <summary>
	/// <see cref="DirectoryAccessor.OpenAppDirectory" />: when no file of the application is found, its folder is opened.
	/// </summary>
	[Test]
	public void OpenAppDirectory_Opens_The_App_Folder_When_No_App_File_Is_Found()
	{
		// Arrange
		IExplorerManager explorerManager = Substitute.For<IExplorerManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppProcess appProcess = Substitute.For<IAppProcess>();

			IPlatform platform = Substitute.For<IPlatform>();

			appProcess
				.BaseDirectory
				.Returns(AppFolder);

			appProcess
				.EntryAssemblyName
				.Returns(AppName);

			platform
				.CurrentOS
				.Returns(OperatingSystemKind.Linux);

			platform
				.IsLinux
				.Returns(true);

			// No real file manager may start from a test.
			explorerManager
				.TryForegroundFolder(Arg.Any<string>(), Arg.Any<string?>())
				.Returns(true);

			builder.RegisterInstance(appProcess);

			builder.RegisterInstance(explorerManager);

			builder.RegisterInstance(platform);

			// An empty disk: the file is not there.
			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();
		});

		DirectoryAccessor sut = mock.Create<DirectoryAccessor>();

		// Act
		sut.OpenAppDirectory();

		// Assert
		explorerManager
			.Received(1)
			.TryForegroundFolder(AppFolder, null);
	}

	/// <summary>
	/// <see cref="DirectoryAccessor.OpenAppDirectory" />: the program that started the process is revealed, also under a
	/// name other than its assembly's, as a portable build has.
	/// </summary>
	[TestCase(OperatingSystemKind.Windows)]
	[TestCase(OperatingSystemKind.Linux)]
	public void OpenAppDirectory_Reveals_The_Program_That_Started_The_Process(OperatingSystemKind os)
	{
		// Arrange
		string processPath = TestPath.Combine(AppFolderName, "DataOrganizer-0.3.0-portable");

		IExplorerManager explorerManager = Substitute.For<IExplorerManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppProcess appProcess = Substitute.For<IAppProcess>();

			MockFileSystem fileSystem = new();

			IPlatform platform = Substitute.For<IPlatform>();

			appProcess
				.BaseDirectory
				.Returns(AppFolder);

			appProcess
				.EntryAssemblyName
				.Returns(AppName);

			appProcess
				.ProcessPath
				.Returns(processPath);

			fileSystem
				.Directory
				.CreateDirectory(Path.GetDirectoryName(processPath)!);

			fileSystem
				.File
				.WriteAllBytes(processPath, []);

			platform
				.CurrentOS
				.Returns(os);

			platform
				.IsLinux
				.Returns(os == OperatingSystemKind.Linux);

			platform
				.IsWindows
				.Returns(os == OperatingSystemKind.Windows);

			// No real file manager may start from a test.
			explorerManager
				.TryForegroundFolder(Arg.Any<string>(), Arg.Any<string?>())
				.Returns(true);

			builder.RegisterInstance(appProcess);

			builder.RegisterInstance(explorerManager);

			builder.RegisterInstance<IFileSystem>(fileSystem);

			builder.RegisterInstance(platform);
		});

		DirectoryAccessor sut = mock.Create<DirectoryAccessor>();

		// Act
		sut.OpenAppDirectory();

		// Assert
		explorerManager
			.Received(1)
			.TryForegroundFolder(AppFolder, processPath);
	}

	/// <summary>
	/// <see cref="DirectoryAccessor.OpenAppDirectory" />: under the shared <c>dotnet</c> host the program next to the
	/// application is revealed, not the host.
	/// </summary>
	[TestCase(OperatingSystemKind.Windows, KnownFileExtensions.Exe)]
	[TestCase(OperatingSystemKind.Linux, "")]
	public void OpenAppDirectory_Under_The_Dotnet_Host_Reveals_The_App_Host(OperatingSystemKind os, string extension)
	{
		// Arrange
		string appHostPath = TestPath.Combine(AppFolderName, AppName + extension);

		IExplorerManager explorerManager = Substitute.For<IExplorerManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppProcess appProcess = Substitute.For<IAppProcess>();

			MockFileSystem fileSystem = new();

			IPlatform platform = Substitute.For<IPlatform>();

			appProcess
				.BaseDirectory
				.Returns(AppFolder);

			appProcess
				.EntryAssemblyName
				.Returns(AppName);

			appProcess
				.ProcessPath
				.Returns(DotnetHostPath);

			// The host exists, so only its name keeps it from being revealed.
			fileSystem
				.Directory
				.CreateDirectory(Path.GetDirectoryName(DotnetHostPath)!);

			fileSystem
				.File
				.WriteAllBytes(DotnetHostPath, []);

			fileSystem
				.Directory
				.CreateDirectory(Path.GetDirectoryName(appHostPath)!);

			fileSystem
				.File
				.WriteAllBytes(appHostPath, []);

			platform
				.CurrentOS
				.Returns(os);

			platform
				.IsLinux
				.Returns(os == OperatingSystemKind.Linux);

			platform
				.IsWindows
				.Returns(os == OperatingSystemKind.Windows);

			// No real file manager may start from a test.
			explorerManager
				.TryForegroundFolder(Arg.Any<string>(), Arg.Any<string?>())
				.Returns(true);

			builder.RegisterInstance(appProcess);

			builder.RegisterInstance(explorerManager);

			builder.RegisterInstance<IFileSystem>(fileSystem);

			builder.RegisterInstance(platform);
		});

		DirectoryAccessor sut = mock.Create<DirectoryAccessor>();

		// Act
		sut.OpenAppDirectory();

		// Assert
		explorerManager
			.Received(1)
			.TryForegroundFolder(AppFolder, appHostPath);
	}

	/// <summary>
	/// <see cref="DirectoryAccessor.OpenAppDirectory" />: under the shared <c>dotnet</c> host, without a program next to
	/// the application, its entry assembly is revealed.
	/// </summary>
	[Test]
	public void OpenAppDirectory_Under_The_Dotnet_Host_Reveals_The_Entry_Assembly()
	{
		// Arrange
		string assemblyPath = TestPath.Combine(AppFolderName, AppName + KnownFileExtensions.Dll);

		IExplorerManager explorerManager = Substitute.For<IExplorerManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppProcess appProcess = Substitute.For<IAppProcess>();

			MockFileSystem fileSystem = new();

			IPlatform platform = Substitute.For<IPlatform>();

			appProcess
				.BaseDirectory
				.Returns(AppFolder);

			appProcess
				.EntryAssemblyName
				.Returns(AppName);

			appProcess
				.ProcessPath
				.Returns(DotnetHostPath);

			// The host exists, so only its name keeps it from being revealed.
			fileSystem
				.Directory
				.CreateDirectory(Path.GetDirectoryName(DotnetHostPath)!);

			fileSystem
				.File
				.WriteAllBytes(DotnetHostPath, []);

			fileSystem
				.Directory
				.CreateDirectory(Path.GetDirectoryName(assemblyPath)!);

			fileSystem
				.File
				.WriteAllBytes(assemblyPath, []);

			platform
				.CurrentOS
				.Returns(OperatingSystemKind.Linux);

			platform
				.IsLinux
				.Returns(true);

			// No real file manager may start from a test.
			explorerManager
				.TryForegroundFolder(Arg.Any<string>(), Arg.Any<string?>())
				.Returns(true);

			builder.RegisterInstance(appProcess);

			builder.RegisterInstance(explorerManager);

			builder.RegisterInstance<IFileSystem>(fileSystem);

			builder.RegisterInstance(platform);
		});

		DirectoryAccessor sut = mock.Create<DirectoryAccessor>();

		// Act
		sut.OpenAppDirectory();

		// Assert
		explorerManager
			.Received(1)
			.TryForegroundFolder(AppFolder, assemblyPath);
	}

	/// <summary>
	/// <see cref="DirectoryAccessor.RevealFile" />: a window that already shows the folder is brought forward with the
	/// file selected, and no other window opens.
	/// </summary>
	[Test]
	public void RevealFile_Brings_An_Open_Window_Of_The_Folder_Forward()
	{
		// Arrange
		IExplorerManager explorerManager = Substitute.For<IExplorerManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			MockFileSystem fileSystem = new();

			fileSystem
				.Directory
				.CreateDirectory(Path.GetDirectoryName(FilePath)!);

			fileSystem
				.File
				.WriteAllBytes(FilePath, []);

			explorerManager
				.TryForegroundFolder(FolderPath, FilePath)
				.Returns(true);

			// No real file manager may start from a test.
			explorerManager
				.TryForegroundFolder(Arg.Any<string>(), null)
				.Returns(true);

			builder.RegisterInstance(explorerManager);

			builder.RegisterInstance<IFileSystem>(fileSystem);
		});

		DirectoryAccessor sut = mock.Create<DirectoryAccessor>();

		// Act
		sut.RevealFile(FilePath);

		// Assert
		explorerManager
			.DidNotReceive()
			.TryRevealFile(Arg.Any<string>());

		explorerManager
			.DidNotReceive()
			.TryForegroundFolder(Arg.Any<string>(), null);
	}

	/// <summary>
	/// <see cref="DirectoryAccessor.RevealFile" />: an empty path reveals nothing.
	/// </summary>
	[Test]
	public void RevealFile_Does_Nothing_For_An_Empty_Path()
	{
		// Arrange
		IExplorerManager explorerManager = Substitute.For<IExplorerManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// No real file manager may start from a test.
			explorerManager
				.TryForegroundFolder(Arg.Any<string>(), null)
				.Returns(true);

			builder.RegisterInstance(explorerManager);
		});

		DirectoryAccessor sut = mock.Create<DirectoryAccessor>();

		// Act
		sut.RevealFile(string.Empty);

		// Assert
		explorerManager
			.DidNotReceiveWithAnyArgs()
			.TryForegroundFolder(default!, default);

		explorerManager
			.DidNotReceiveWithAnyArgs()
			.TryRevealFile(default!);
	}

	/// <summary>
	/// <see cref="DirectoryAccessor.RevealFile" />: without such a window a new one opens with the file selected, and the
	/// folder alone is not opened.
	/// </summary>
	[Test]
	public void RevealFile_Opens_A_Window_With_The_File_Selected()
	{
		// Arrange
		IExplorerManager explorerManager = Substitute.For<IExplorerManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			MockFileSystem fileSystem = new();

			fileSystem
				.Directory
				.CreateDirectory(Path.GetDirectoryName(FilePath)!);

			fileSystem
				.File
				.WriteAllBytes(FilePath, []);

			explorerManager
				.TryRevealFile(FilePath)
				.Returns(true);

			// No real file manager may start from a test.
			explorerManager
				.TryForegroundFolder(Arg.Any<string>(), null)
				.Returns(true);

			builder.RegisterInstance(explorerManager);

			builder.RegisterInstance<IFileSystem>(fileSystem);
		});

		DirectoryAccessor sut = mock.Create<DirectoryAccessor>();

		// Act
		sut.RevealFile(FilePath);

		// Assert
		explorerManager
			.Received(1)
			.TryRevealFile(FilePath);

		explorerManager
			.DidNotReceive()
			.TryForegroundFolder(Arg.Any<string>(), null);
	}

	/// <summary>
	/// <see cref="DirectoryAccessor.RevealFile" />: a file that is gone leaves its folder to be opened, without a try to
	/// select the file.
	/// </summary>
	[Test]
	public void RevealFile_Opens_The_Folder_Of_A_File_That_Is_Gone()
	{
		// Arrange
		IExplorerManager explorerManager = Substitute.For<IExplorerManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// No real file manager may start from a test.
			explorerManager
				.TryForegroundFolder(Arg.Any<string>(), null)
				.Returns(true);

			builder.RegisterInstance(explorerManager);

			// An empty disk: the file is not there.
			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();
		});

		DirectoryAccessor sut = mock.Create<DirectoryAccessor>();

		// Act
		sut.RevealFile(FilePath);

		// Assert
		explorerManager
			.DidNotReceive()
			.TryForegroundFolder(FolderPath, FilePath);

		explorerManager
			.Received(1)
			.TryForegroundFolder(FolderPath, null);
	}

	/// <summary>
	/// <see cref="DirectoryAccessor.RevealFile" />: when neither an open window nor a new one can select the file, its
	/// folder is opened.
	/// </summary>
	[Test]
	public void RevealFile_Opens_The_Folder_When_The_File_Cannot_Be_Selected()
	{
		// Arrange
		IExplorerManager explorerManager = Substitute.For<IExplorerManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			MockFileSystem fileSystem = new();

			fileSystem
				.Directory
				.CreateDirectory(Path.GetDirectoryName(FilePath)!);

			fileSystem
				.File
				.WriteAllBytes(FilePath, []);

			// No real file manager may start from a test.
			explorerManager
				.TryForegroundFolder(Arg.Any<string>(), null)
				.Returns(true);

			builder.RegisterInstance(explorerManager);

			builder.RegisterInstance<IFileSystem>(fileSystem);
		});

		DirectoryAccessor sut = mock.Create<DirectoryAccessor>();

		// Act
		sut.RevealFile(FilePath);

		// Assert
		explorerManager
			.Received(1)
			.TryForegroundFolder(FolderPath, null);
	}

	/// <summary>
	/// <see cref="DirectoryAccessor.RevealFile" />: a folder, such as an app bundle of macOS, is revealed in its parent
	/// as a file is.
	/// </summary>
	[Test]
	public void RevealFile_Takes_A_Folder_As_Well()
	{
		// Arrange
		IExplorerManager explorerManager = Substitute.For<IExplorerManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			MockFileSystem fileSystem = new();

			fileSystem
				.Directory
				.CreateDirectory(FilePath);

			explorerManager
				.TryForegroundFolder(FolderPath, FilePath)
				.Returns(true);

			// No real file manager may start from a test.
			explorerManager
				.TryForegroundFolder(Arg.Any<string>(), null)
				.Returns(true);

			builder.RegisterInstance(explorerManager);

			builder.RegisterInstance<IFileSystem>(fileSystem);
		});

		DirectoryAccessor sut = mock.Create<DirectoryAccessor>();

		// Act
		sut.RevealFile(FilePath);

		// Assert
		explorerManager
			.Received(1)
			.TryForegroundFolder(FolderPath, FilePath);
	}
	#endregion
}
