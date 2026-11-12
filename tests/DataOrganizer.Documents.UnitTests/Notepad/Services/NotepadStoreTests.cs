using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Documents.Editor.Dto;
using DataOrganizer.Documents.Notepad.Dto;
using DataOrganizer.Documents.Notepad.Services;
using DataOrganizer.Infrastructure.AppInstance.Interfaces;
using DataOrganizer.Shared.Interfaces;
using DataOrganizer.Shared.Services;
using DataOrganizer.TestSupport.Common;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using Testably.Abstractions.Testing;

namespace DataOrganizer.Documents.UnitTests.Notepad.Services;

[TestFixture(Description = $@"Tests of ""{nameof(NotepadStore)}"" type")]
internal class NotepadStoreTests
{
	#region Data
	/// <summary>
	/// Name of the directory the texts of the tabs live in.
	/// </summary>
	private const string NotepadFolderName = "notepad";

	/// <summary>
	/// Name of the directory the settings live in.
	/// </summary>
	private const string SettingsFolderName = "settings";

	/// <summary>
	/// Directory the texts of the tabs live in.
	/// </summary>
	private static readonly string NotepadFolder = TestPath.Combine(NotepadFolderName);
	/// <summary>
	/// Directory the settings live in.
	/// </summary>
	private static readonly string SettingsFolder = TestPath.Combine(SettingsFolderName);

	/// <summary>
	/// Path of the file the settings of the tabs live in.
	/// </summary>
	private static readonly string SettingsPath = TestPath.Combine(SettingsFolderName, $"{nameof(NotepadViewSettings)}.json");

	/// <summary>
	/// Path of the text of the tab numbered 3.
	/// </summary>
	private static readonly string TabThreeTextPath = TestPath.Combine(NotepadFolderName, "3.txt");

	/// <summary>
	/// Path of the text of the tab numbered 2.
	/// </summary>
	private static readonly string TabTwoTextPath = TestPath.Combine(NotepadFolderName, "2.txt");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="NotepadStore.Erase" />: a tab without a file of its text has nothing to erase.
	/// </summary>
	[Test]
	public void Erase_Leaves_A_Missing_File_Alone()
	{
		// Arrange
		IFileEraser fileEraser = Substitute.For<IFileEraser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance(fileEraser);

			// One file system for all the services, as they share one disk in the app.
			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		sut.Erase(2);

		// Assert
		fileEraser
			.DidNotReceive()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="NotepadStore.Erase" />: the file of the text of the tab goes away.
	/// </summary>
	[Test]
	public void Erase_Takes_The_File_Of_The_Text_Away()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(NotepadFolder);

		files
			.File
			.WriteAllBytes(TabTwoTextPath, [1, 2, 3]);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance<IFileSystem>(files);

			builder
				.RegisterType<AtomicFileWriter>()
				.As<IAtomicFileWriter>();

			builder
				.RegisterType<FileEraser>()
				.As<IFileEraser>();
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		sut.Erase(2);

		// Assert
		files.File.Exists(TabTwoTextPath)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="NotepadStore.FindNumbers" />: a folder that is not there is not searched.
	/// </summary>
	[Test]
	public void FindNumbers_Does_Not_Search_A_Missing_Folder()
	{
		// Arrange
		MockFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance<IFileSystem>(files);
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		sut.FindNumbers();

		// Assert
		files.Statistics.Directory.Methods
			.Should()
			.NotContain(x => x.Name == nameof(IDirectory.EnumerateFiles));
	}

	/// <summary>
	/// <see cref="NotepadStore.FindNumbers" />: a folder that cannot be searched holds no text.
	/// </summary>
	[Test]
	public void FindNumbers_Returns_Nothing_When_The_Folder_Cannot_Be_Read()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			// The mock cannot fail the listing of a folder, so the folder is faked: it exists, and listing it fails.
			fileSystem
				.Directory
				.Exists(NotepadFolder)
				.Returns(true);

			fileSystem
				.Directory
				.EnumerateFiles(NotepadFolder)
				.Throws(new IOException());

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance(fileSystem);
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		int[] result = sut.FindNumbers();

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="NotepadStore.FindNumbers" />: before the first text is written there is no folder, and no text.
	/// </summary>
	[Test]
	public void FindNumbers_Returns_Nothing_Without_The_Folder()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			builder.RegisterInstance(appEnvironment);

			// One file system for all the services, as they share one disk in the app.
			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();

			builder
				.RegisterType<AtomicFileWriter>()
				.As<IAtomicFileWriter>();

			builder
				.RegisterType<FileEraser>()
				.As<IFileEraser>();
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		int[] result = sut.FindNumbers();

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="NotepadStore.FindNumbers" />: only the files named by the number of a tab count, so no two files stand
	/// for one tab.
	/// </summary>
	[Test]
	public void FindNumbers_Returns_The_Numbers_Of_The_Texts()
	{
		// Arrange
		string[] names = ["1.txt", "12.txt", "01.txt", "0.txt", "3.txt.tmp", "a.txt", "4.json", "5.TXT"];

		MockFileSystem files = new();

		foreach (string name in names)
		{
			files
				.Directory
				.CreateDirectory(NotepadFolder);

			files
				.File
				.WriteAllBytes(Path.Combine(NotepadFolder, name), []);
		}

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance<IFileSystem>(files);

			builder
				.RegisterType<AtomicFileWriter>()
				.As<IAtomicFileWriter>();

			builder
				.RegisterType<FileEraser>()
				.As<IFileEraser>();
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		int[] result = sut.FindNumbers();

		// Assert
		result
			.Should()
			.BeEquivalentTo([1, 12]);
	}

	/// <summary>
	/// <see cref="NotepadStore.Read" />: a tab without a file has an empty text, not a text that cannot be read.
	/// </summary>
	[Test]
	public void Read_Returns_No_Bytes_For_A_Tab_Without_A_Text()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			builder.RegisterInstance(appEnvironment);

			// One file system for all the services, as they share one disk in the app.
			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();

			builder
				.RegisterType<AtomicFileWriter>()
				.As<IAtomicFileWriter>();

			builder
				.RegisterType<FileEraser>()
				.As<IFileEraser>();
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		byte[]? result = sut.Read(5);

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="NotepadStore.Read" />: a file that cannot be read gives no bytes at all.
	/// </summary>
	[Test]
	public void Read_Returns_Nothing_When_The_Text_Cannot_Be_Read()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(NotepadFolder);

		files
			.File
			.WriteAllBytes(TabTwoTextPath, [1, 2, 3]);

		// Another program keeps the file to itself.
		using FileSystemStream holder = files
			.File
			.Open(
				TabTwoTextPath,
				FileMode.Open,
				FileAccess.ReadWrite,
				FileShare.None);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance<IFileSystem>(files);
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		byte[]? result = sut.Read(2);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="NotepadStore.Read" />: the bytes of the file of the text come back as they are.
	/// </summary>
	[Test]
	public void Read_Returns_The_Bytes_Of_The_Text()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(NotepadFolder);

		files
			.File
			.WriteAllBytes(TabTwoTextPath, [1, 2, 3]);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance<IFileSystem>(files);

			builder
				.RegisterType<AtomicFileWriter>()
				.As<IAtomicFileWriter>();

			builder
				.RegisterType<FileEraser>()
				.As<IFileEraser>();
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		byte[]? result = sut.Read(2);

		// Assert
		result
			.Should()
			.Equal(1, 2, 3);
	}

	/// <summary>
	/// <see cref="NotepadStore.ReadSettings" />: a file of the settings that is not there is not read.
	/// </summary>
	[Test]
	public void ReadSettings_Does_Not_Read_A_Missing_File()
	{
		// Arrange
		MockFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.GetSettingsFilePath(nameof(NotepadViewSettings))
				.Returns(SettingsPath);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance<IFileSystem>(files);
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		sut.ReadSettings();

		// Assert
		files.Statistics.File.Methods
			.Should()
			.NotContain(x => x.Name == nameof(IFile.ReadAllBytes));
	}

	/// <summary>
	/// <see cref="NotepadStore.ReadSettings" />: settings that cannot be read, such as a file cut short, give none.
	/// </summary>
	[Test]
	public void ReadSettings_Returns_Nothing_When_The_Settings_Cannot_Be_Read()
	{
		// Arrange
		MockFileSystem files = new();

		files
			.Directory
			.CreateDirectory(SettingsFolder);

		files
			.File
			.WriteAllBytes(SettingsPath, Encoding.UTF8.GetBytes("""{ "SelectedTabNumber": 1, "Tabs": ["""));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.GetSettingsFilePath(nameof(NotepadViewSettings))
				.Returns(SettingsPath);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance<IFileSystem>(files);

			builder
				.RegisterType<AtomicFileWriter>()
				.As<IAtomicFileWriter>();

			builder
				.RegisterType<FileEraser>()
				.As<IFileEraser>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		NotepadViewSettings? result = sut.ReadSettings();

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="NotepadStore.ReadSettings" />: before the first write there are no settings.
	/// </summary>
	[Test]
	public void ReadSettings_Returns_Nothing_Without_A_File()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.GetSettingsFilePath(nameof(NotepadViewSettings))
				.Returns(SettingsPath);

			builder.RegisterInstance(appEnvironment);

			// One file system for all the services, as they share one disk in the app.
			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();

			builder
				.RegisterType<AtomicFileWriter>()
				.As<IAtomicFileWriter>();

			builder
				.RegisterType<FileEraser>()
				.As<IFileEraser>();
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		NotepadViewSettings? result = sut.ReadSettings();

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="NotepadStore.ReadSettings" />: the settings come back as they were written, with the state of the editor
	/// of each tab and its split.
	/// </summary>
	[Test]
	public void ReadSettings_Returns_The_Written_Settings()
	{
		// Arrange
		NotepadViewSettings settings = new()
		{
			SelectedTabNumber = 2,
			Tabs =
			[
				new()
				{
					EditorState = new FileEditorState
					{
						Bookmarks = [3, 5],
						CaretPosition = new()
						{
							Column = 4,
							Line = 2,
							VisualColumn = -1
						},
						Encoding = Encoding.Unicode.WebName,
						FoldedBlocks = [10],
						FontSize = 16.0,
						ScrollOffset = new(0, 120),
						SelectionLength = 3,
						SelectionStart = 7,
						ShowEndOfLine = true,
						ShowSpaces = true,
						ShowTabs = true,
						SyntaxLanguage = "csharp",
						WordWrap = true
					},
					Name = "Notes",
					Number = 1,
					Split = 0.3
				},
				new()
				{
					EditorState = null,
					Name = null,
					Number = 2,
					Split = null
				}
			]
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.GetSettingsFilePath(nameof(NotepadViewSettings))
				.Returns(SettingsPath);

			builder.RegisterInstance(appEnvironment);

			// One file system for all the services, as they share one disk in the app.
			builder
				.RegisterType<MockFileSystem>()
				.As<IFileSystem>()
				.SingleInstance();

			builder
				.RegisterType<AtomicFileWriter>()
				.As<IAtomicFileWriter>();

			builder
				.RegisterType<FileEraser>()
				.As<IFileEraser>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		sut.WriteSettings(settings);

		// Act
		NotepadViewSettings? result = sut.ReadSettings();

		// Assert
		result
			.Should()
			.BeEquivalentTo(
				settings,
				options => options
					.ComparingByMembers<FileEditorState>()
					.WithStrictOrdering());
	}

	/// <summary>
	/// <see cref="NotepadStore.Write" />: the first text makes the folder of the texts.
	/// </summary>
	[Test]
	public void Write_Makes_The_Folder_Of_The_Texts()
	{
		// Arrange
		MockFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance<IFileSystem>(files);

			builder
				.RegisterType<AtomicFileWriter>()
				.As<IAtomicFileWriter>();

			builder
				.RegisterType<FileEraser>()
				.As<IFileEraser>();
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		sut.Write(3, [4, 5, 6]);

		// Assert
		files.Directory.Exists(NotepadFolder)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadStore.Write" />: the bytes take the place of the file of the text at once, so a write cut short
	/// leaves the old text.
	/// </summary>
	[Test]
	public void Write_Puts_The_Bytes_In_Place_Atomically()
	{
		// Arrange
		MockFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance<IFileSystem>(files);

			builder
				.RegisterType<AtomicFileWriter>()
				.As<IAtomicFileWriter>();

			builder
				.RegisterType<FileEraser>()
				.As<IFileEraser>();
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		bool result = sut.Write(3, [4, 5, 6]);

		// Assert
		result
			.Should()
			.BeTrue();

		files.Statistics.File.Methods
			.Should()
			.ContainSingle(x => x.Name == nameof(IFile.Move) && x.Parameters[1].Is(TabThreeTextPath));

		files.File.ReadAllBytes(TabThreeTextPath)
			.Should()
			.Equal(4, 5, 6);
	}

	/// <summary>
	/// <see cref="NotepadStore.Write" />: a write that fails says so instead of throwing.
	/// </summary>
	[Test]
	public void Write_Returns_False_When_The_Text_Cannot_Be_Written()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			IAtomicFileWriter atomicFileWriter = Substitute.For<IAtomicFileWriter>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			atomicFileWriter
				.When(x => x.WriteAllBytes(TabThreeTextPath, Arg.Any<byte[]>()))
				.Throw(new IOException());

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance(atomicFileWriter);
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		bool result = sut.Write(3, [4, 5, 6]);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="NotepadStore.WriteSettings" />: a write that fails does not throw.
	/// </summary>
	[Test]
	public void WriteSettings_Does_Not_Throw_When_The_Settings_Cannot_Be_Written()
	{
		// Arrange
		NotepadViewSettings settings = new()
		{
			SelectedTabNumber = 1,
			Tabs =
			[
				new()
				{
					EditorState = null,
					Name = null,
					Number = 1,
					Split = null
				}
			]
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			IAtomicFileWriter atomicFileWriter = Substitute.For<IAtomicFileWriter>();

			appEnvironment
				.GetSettingsFilePath(nameof(NotepadViewSettings))
				.Returns(SettingsPath);

			atomicFileWriter
				.When(x => x.WriteAllBytes(SettingsPath, Arg.Any<byte[]>()))
				.Throw(new IOException());

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance(atomicFileWriter);
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		Action act = () => sut.WriteSettings(settings);

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="NotepadStore.WriteSettings" />: the first settings make the folder of the settings.
	/// </summary>
	[Test]
	public void WriteSettings_Makes_The_Folder_Of_The_Settings()
	{
		// Arrange
		NotepadViewSettings settings = new()
		{
			SelectedTabNumber = 1,
			Tabs = []
		};

		MockFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.GetSettingsFilePath(nameof(NotepadViewSettings))
				.Returns(SettingsPath);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance<IFileSystem>(files);

			builder
				.RegisterType<AtomicFileWriter>()
				.As<IAtomicFileWriter>();

			builder
				.RegisterType<FileEraser>()
				.As<IFileEraser>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		sut.WriteSettings(settings);

		// Assert
		files.Directory.Exists(SettingsFolder)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadStore.WriteSettings" />: the settings take the place of their file at once, so a write cut short
	/// leaves the old ones.
	/// </summary>
	[Test]
	public void WriteSettings_Puts_The_Settings_In_Place_Atomically()
	{
		// Arrange
		NotepadViewSettings settings = new()
		{
			SelectedTabNumber = 1,
			Tabs =
			[
				new()
				{
					EditorState = null,
					Name = null,
					Number = 1,
					Split = null
				}
			]
		};

		MockFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.GetSettingsFilePath(nameof(NotepadViewSettings))
				.Returns(SettingsPath);

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance<IFileSystem>(files);

			builder
				.RegisterType<AtomicFileWriter>()
				.As<IAtomicFileWriter>();

			builder
				.RegisterType<FileEraser>()
				.As<IFileEraser>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		sut.WriteSettings(settings);

		// Assert
		files.Statistics.File.Methods
			.Should()
			.ContainSingle(x => x.Name == nameof(IFile.Move) && x.Parameters[1].Is(SettingsPath));
	}
	#endregion
}
