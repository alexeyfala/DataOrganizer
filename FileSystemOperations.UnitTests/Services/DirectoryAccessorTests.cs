using Autofac;
using Autofac.Extras.Moq;
using FileSystemOperations.Interfaces;
using FileSystemOperations.Services;
using NSubstitute;
using Shared.Interfaces;
using TestSupport.Common;

namespace FileSystemOperations.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(DirectoryAccessor)}"" type")]
internal class DirectoryAccessorTests
{
	#region Data
	/// <summary>
	/// Name of the folder that holds the file.
	/// </summary>
	private const string FolderName = "Docs";

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
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.FileExists(FilePath)
				.Returns(true);

			explorerManager
				.TryForegroundFolder(FolderPath, FilePath)
				.Returns(true);

			// No real file manager may start from a test.
			explorerManager
				.TryForegroundFolder(Arg.Any<string>(), null)
				.Returns(true);

			builder.RegisterInstance(explorerManager);

			builder.RegisterInstance(fileSystem);
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
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.FileExists(FilePath)
				.Returns(true);

			explorerManager
				.TryRevealFile(FilePath)
				.Returns(true);

			// No real file manager may start from a test.
			explorerManager
				.TryForegroundFolder(Arg.Any<string>(), null)
				.Returns(true);

			builder.RegisterInstance(explorerManager);

			builder.RegisterInstance(fileSystem);
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
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.FileExists(FilePath)
				.Returns(true);

			// No real file manager may start from a test.
			explorerManager
				.TryForegroundFolder(Arg.Any<string>(), null)
				.Returns(true);

			builder.RegisterInstance(explorerManager);

			builder.RegisterInstance(fileSystem);
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
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			fileSystem
				.DirectoryExists(FilePath)
				.Returns(true);

			explorerManager
				.TryForegroundFolder(FolderPath, FilePath)
				.Returns(true);

			// No real file manager may start from a test.
			explorerManager
				.TryForegroundFolder(Arg.Any<string>(), null)
				.Returns(true);

			builder.RegisterInstance(explorerManager);

			builder.RegisterInstance(fileSystem);
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
