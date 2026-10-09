using Autofac;
using Autofac.Extras.Moq;
using AvaloniaEdit;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Services.Notepad;
using DataOrganizer.UnitTests.Fakes;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.IO;
using System.Text;

namespace DataOrganizer.UnitTests.Services.Notepad;

[TestFixture(Description = $@"Tests of ""{nameof(NotepadStore)}"" type")]
internal class NotepadStoreTests
{
	#region Data
	/// <summary>
	/// Directory the texts of the tabs live in.
	/// </summary>
	private const string NotepadFolder = "notepad";

	/// <summary>
	/// Directory the settings of the tabs live in.
	/// </summary>
	private const string SettingsFolder = "settings";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="NotepadStore.Erase" />: the file of the text of the tab goes away.
	/// </summary>
	[Test]
	public void Erase_Takes_The_File_Of_The_Text_Away()
	{
		// Arrange
		string filePath = Path.Combine(NotepadFolder, "2.txt");

		InMemoryFileSystem files = new();

		files.Files[filePath] = [1, 2, 3];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		sut.Erase(2);

		// Assert
		files.Files
			.Should()
			.NotContainKey(filePath);
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

			builder
				.RegisterType<InMemoryFileSystem>()
				.As<IFileSystem>();
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

		InMemoryFileSystem files = new();

		foreach (string name in names)
		{
			files.Files[Path.Combine(NotepadFolder, name)] = [];
		}

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();
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

			builder
				.RegisterType<InMemoryFileSystem>()
				.As<IFileSystem>();
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
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			string filePath = Path.Combine(NotepadFolder, "2.txt");

			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			fileSystem
				.FileExists(filePath)
				.Returns(true);

			fileSystem
				.ReadAllBytes(filePath)
				.Throws(new IOException());

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance(fileSystem);
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
		InMemoryFileSystem files = new();

		files.Files[Path.Combine(NotepadFolder, "2.txt")] = [1, 2, 3];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();
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
	/// <see cref="NotepadStore.ReadSettings" />: settings that cannot be read, such as a file cut short, give none.
	/// </summary>
	[Test]
	public void ReadSettings_Returns_Nothing_When_The_Settings_Cannot_Be_Read()
	{
		// Arrange
		string filePath = Path.Combine(SettingsFolder, $"{nameof(NotepadViewSettings)}.json");

		InMemoryFileSystem files = new();

		files.Files[filePath] = Encoding.UTF8.GetBytes("""{ "SelectedTabNumber": 1, "Tabs": [""");

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.GetSettingsFilePath(nameof(NotepadViewSettings))
				.Returns(filePath);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

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
				.Returns(Path.Combine(SettingsFolder, $"{nameof(NotepadViewSettings)}.json"));

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterType<InMemoryFileSystem>()
				.As<IFileSystem>();
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
						CaretPosition = new TextViewPosition(2, 4),
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
				.Returns(Path.Combine(SettingsFolder, $"{nameof(NotepadViewSettings)}.json"));

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterType<InMemoryFileSystem>()
				.As<IFileSystem>();

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
	/// <see cref="NotepadStore.Write" />: the bytes take the place of the file of the text at once, so a write cut short
	/// leaves the old text.
	/// </summary>
	[Test]
	public void Write_Puts_The_Bytes_In_Place_Atomically()
	{
		// Arrange
		string filePath = Path.Combine(NotepadFolder, "3.txt");

		InMemoryFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		bool result = sut.Write(3, [4, 5, 6]);

		// Assert
		result
			.Should()
			.BeTrue();

		files.AtomicWrites
			.Should()
			.Equal(filePath);

		files.Files[filePath]
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

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			appEnvironment
				.NotepadDirectoryPath
				.Returns(NotepadFolder);

			fileSystem
				.When(x => x.WriteAllBytesAtomic(Path.Combine(NotepadFolder, "3.txt"), Arg.Any<byte[]>()))
				.Throw(new IOException());

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance(fileSystem);
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
			string filePath = Path.Combine(SettingsFolder, $"{nameof(NotepadViewSettings)}.json");

			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			appEnvironment
				.GetSettingsFilePath(nameof(NotepadViewSettings))
				.Returns(filePath);

			fileSystem
				.When(x => x.WriteAllBytesAtomic(filePath, Arg.Any<byte[]>()))
				.Throw(new IOException());

			builder.RegisterInstance(appEnvironment);

			builder.RegisterInstance(fileSystem);
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
	/// <see cref="NotepadStore.WriteSettings" />: the settings take the place of their file at once, so a write cut short
	/// leaves the old ones.
	/// </summary>
	[Test]
	public void WriteSettings_Puts_The_Settings_In_Place_Atomically()
	{
		// Arrange
		string filePath = Path.Combine(SettingsFolder, $"{nameof(NotepadViewSettings)}.json");

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

		InMemoryFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppEnvironment appEnvironment = Substitute.For<IAppEnvironment>();

			appEnvironment
				.GetSettingsFilePath(nameof(NotepadViewSettings))
				.Returns(filePath);

			builder.RegisterInstance(appEnvironment);

			builder
				.RegisterInstance(files)
				.As<IFileSystem>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		NotepadStore sut = mock.Create<NotepadStore>();

		// Act
		sut.WriteSettings(settings);

		// Assert
		files.AtomicWrites
			.Should()
			.Equal(filePath);
	}
	#endregion
}
