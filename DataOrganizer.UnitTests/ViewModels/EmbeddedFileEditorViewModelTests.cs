using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Text;
using DataOrganizer.Messages.Editor;
using DataOrganizer.Services.Documents;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.ViewModels;
using Entities.Models;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Dto;
using Repository.Enums;
using Repository.Interfaces;
using Repository.Interfaces.Database;
using Repository.Services;
using Repository.Services.Database;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using TestSupport.Common;
using TestSupport.Database;
using TestSupport.Models;

namespace DataOrganizer.UnitTests.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(EmbeddedFileEditorViewModel)}"" type")]
internal class EmbeddedFileEditorViewModelTests
{
	#region Data
	/// <summary>
	/// Editor state of a file as an earlier build stored it, with every field set, the caret position of the engine
	/// included.
	/// </summary>
	private const string RecordedEditorState = """
		{
		  "Bookmarks": [
		    3,
		    7
		  ],
		  "CaretPosition": {
		    "Location": {
		      "Line": 3,
		      "Column": 5,
		      "IsEmpty": false
		    },
		    "Line": 3,
		    "Column": 5,
		    "VisualColumn": 4,
		    "IsAtEndOfLine": true
		  },
		  "FoldedBlocks": [
		    12,
		    40
		  ],
		  "FontSize": 16.5,
		  "ScrollOffset": {
		    "IsEmpty": false,
		    "X": 15,
		    "Y": 480
		  },
		  "SelectionLength": 4,
		  "SelectionStart": 20,
		  "ShowEndOfLine": true,
		  "ShowSpaces": true,
		  "ShowTabs": true,
		  "SyntaxLanguage": "python",
		  "UnfoldedBlocks": [
		    25
		  ],
		  "WordWrap": true
		}
		""";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: closing the editor queues the edit still waiting for a pause in
	/// typing, and the queue encrypts it although it gets to it only after the close.
	/// </summary>
	[AvaloniaTest]
	public async Task Dispose_Saves_The_Edit_Waiting_For_A_Pause_Encrypted()
	{
		// Arrange
		byte[]? saved = null;

		TaskCompletionSource writing = new(TaskCreationOptions.RunContinuationsAsynchronously);

		TaskCompletionSource<bool> written = new(TaskCreationOptions.RunContinuationsAsynchronously);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = "Hello"u8.ToArray(),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			// The writes wait for the test, so the queue gets to the edit queued on closing only after the close.
			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(_ =>
				{
					writing.TrySetResult();

					return written.Task;
				});

			// An encrypted file hands its plain text to the cipher; the copy survives the wipe after the save.
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(x => x.ArgAt<byte[]>(2));

			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Do<byte[]>(x => saved = [.. x]))
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder.RegisterInstance(contentCipher);
		});

		EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: Guid.NewGuid()));

		await sut.EditorLoaded();

		sut.Editor.Document.Text = "Hello, world";

		FlushEditorsMessage flush = new();

		sut.Receive(flush);

		// The queue is now busy with the save of the first edit.
		await writing.Task;

		sut.Editor.Document.Text = "Hello, world!";

		// Act
		sut.Dispose();

		written.SetResult(true);

		// The flush answers once the queue is empty, the edit queued on closing included.
		await flush.GetResponsesAsync();

		// Assert
		byte[] expected = "Hello, world!"u8.ToArray();

		saved
			.Should()
			.Equal(expected);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: the stored state reaches the bound properties
	/// on the UI thread, also when the database answers from another thread.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Applies_The_Stored_State_On_The_UI_Thread()
	{
		// Arrange
		TaskCompletionSource<string?> stateRead = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(stateRead.Task);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		bool? isOnUiThread = null;

		sut.Editor.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(DocumentEditorViewModel.ViewState))
			{
				isOnUiThread = Dispatcher.UIThread.CheckAccess();
			}
		};

		string json = new SystemTextJsonSerializer().Serialize(new FileEditorState
		{
			CaretPosition = new()
			{
				Column = 2,
				Line = 3,
				VisualColumn = -1
			},
			FontSize = 20.0,
			ScrollOffset = new(15, 480),
			SelectionLength = 4,
			SelectionStart = 20,
			WordWrap = true
		});

		Task loading = sut.EditorLoaded();

		// Act
		await Task.Run(() => stateRead.SetResult(json));

		await loading;

		// Assert
		isOnUiThread
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: contents that fail their check close the editor for
	/// changes.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Closes_The_Editor_When_The_Contents_Are_Damaged()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = false
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.IsContentUnavailable
			.Should()
			.BeTrue();

		sut.IsEditingEnabled
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: contents that are not text close the editor for changes,
	/// since saving them back as text would rewrite the file.
	/// </summary>
	[AvaloniaTest]
	[TestCaseSource(nameof(NonTextContents))]
	public async Task EditorLoaded_Closes_The_Editor_When_The_Contents_Are_Not_Text(byte[] contents)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = [.. contents],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.IsContentUnavailable
			.Should()
			.BeTrue();

		sut.IsEditingEnabled
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: contents the session key does not open close the editor
	/// for changes before its state is read.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Closes_The_Editor_When_The_Decryption_Fails()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents fileContents = new()
			{
				Contents = RandomValues.CreateBytes(10),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			// The key does not open the contents.
			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), ContentIdentity.Contents, Arg.Any<byte[]>())
				.Returns((byte[]?)null);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(contentCipher);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: Guid.NewGuid()));

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.IsContentUnavailable
			.Should()
			.BeTrue();

		await dbAccess
			.DidNotReceive()
			.GetFileEditorStateAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: a read the database could not answer closes
	/// the editor for changes.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Closes_The_Editor_When_The_Read_Fails()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.IsContentUnavailable
			.Should()
			.BeTrue();

		sut.IsEditingEnabled
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: the text of the file fills the document the editor starts
	/// with, which the view already shows.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Fills_The_Document_It_Starts_With()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = Encoding.UTF8.GetBytes("Hello"),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		TextDocument document = sut.Editor.Document;

		// Act
		await sut.EditorLoaded();

		// Assert
		document.Text
			.Should()
			.Be("Hello");
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: the loaded text is not an edit, so there is nothing to undo.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Leaves_Nothing_To_Undo()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.Document.UndoStack.CanUndo
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: loads the file contents into the document and applies the stored editor state (font size, word wrap).
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Loads_Text_Into_Document()
	{
		// Arrange
		byte[] contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10));

		double fontSize = RandomValues.CreateDouble(6.0, 64.0);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = [.. contents],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			FileEditorState state = new()
			{
				CaretPosition = default,
				FontSize = fontSize,
				WordWrap = true,
				ScrollOffset = default,
				SelectionLength = default,
				SelectionStart = default
			};

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(new SystemTextJsonSerializer().Serialize(state));

			IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

			serializer
				.Deserialize<FileEditorState>(Arg.Any<string>())
				.Returns(state);

			builder.RegisterInstance(serializer);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.IsInitialized
			.Should()
			.BeTrue();

		sut.Editor.Document.Text
			.Should()
			.Be(TextDefaults.Encoding.GetString(contents));

		sut.Editor.WordWrap
			.Should()
			.BeTrue();

		sut.Editor.FontSize
			.Should()
			.Be(fontSize);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: the contents of a protected file reach the document opened
	/// with the session key of its keeper.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Loads_The_Decrypted_Text()
	{
		// Arrange
		string text = RandomString.Create(10);

		Guid keeperId = Guid.NewGuid();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			byte[] stored = RandomValues.CreateBytes(16);

			ValidatedContents fileContents = new()
			{
				Contents = stored,
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(keeperId, ContentIdentity.Contents, stored)
				.Returns(TextDefaults.Encoding.GetBytes(text));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(contentCipher);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: keeperId));

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.Document.Text
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: names the encoding of the file, telling apart a file
	/// that starts with a byte order mark.
	/// </summary>
	[AvaloniaTest]
	[TestCase(new byte[] { 0x41 }, "UTF-8")]
	[TestCase(new byte[] { 0xEF, 0xBB, 0xBF, 0x41 }, "UTF-8-BOM")]
	[TestCase(new byte[] { 0xFF, 0xFE, 0x41, 0x00 }, "UTF-16 LE BOM")]
	[TestCase(new byte[] { 0xFE, 0xFF, 0x00, 0x41 }, "UTF-16 BE BOM")]
	[TestCase(new byte[] { 0x41, 0x00 }, "UTF-16 LE")]
	[TestCase(new byte[] { 0x00, 0x41 }, "UTF-16 BE")]
	public async Task EditorLoaded_Names_The_Encoding(byte[] contents, string expected)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = [.. contents],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.EncodingName
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: contents read successfully open the document for editing.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Opens_The_Document_For_Editing()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		sut.IsEditingEnabled
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: a state stored by an earlier build reaches every property
	/// and is not written over.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Reads_A_State_Recorded_By_An_Earlier_Build()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(RecordedEditorState);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterInstance(SyntaxRegistry.Instance)
				.As<ISyntaxRegistry>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.FontSize
			.Should()
			.Be(16.5);

		sut.Editor.ShowEndOfLine
			.Should()
			.BeTrue();

		sut.Editor.ShowSpaces
			.Should()
			.BeTrue();

		sut.Editor.ShowTabs
			.Should()
			.BeTrue();

		sut.Editor.SyntaxLanguage
			.Should()
			.Be("python");

		sut.Editor.ViewState
			.Should()
			.Be(new DocumentViewState
			{
				Bookmarks = [3, 7],
				CaretPosition = new(line: 3, column: 5, visualColumn: 4)
				{
					IsAtEndOfLine = true
				},
				FoldedBlocks = [12, 40],
				ScrollOffset = new(15.0, 480.0),
				SelectionLength = 4,
				SelectionStart = 20,
				UnfoldedBlocks = [25]
			});

		sut.Editor.WordWrap
			.Should()
			.BeTrue();

		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFileEditorStateAsync(default, default!, default);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: a state saved before its optional fields appeared
	/// is read as it is and not written over.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Reads_A_State_Without_The_Optional_Fields()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			JsonObject state = JsonNode
				.Parse(new SystemTextJsonSerializer().Serialize(new FileEditorState
				{
					CaretPosition = default,
					FontSize = 20.0,
					ScrollOffset = default,
					SelectionLength = default,
					SelectionStart = default,
					WordWrap = default
				}))!
				.AsObject();

			state.Remove(nameof(FileEditorState.ShowEndOfLine));

			state.Remove(nameof(FileEditorState.ShowSpaces));

			state.Remove(nameof(FileEditorState.ShowTabs));

			state.Remove(nameof(FileEditorState.SyntaxLanguage));

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(state.ToJsonString());

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.FontSize
			.Should()
			.Be(20.0);

		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFileEditorStateAsync(default, default!, default);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: a file that is not UTF-8 opens for editing in the fallback
	/// code page.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Reads_A_Text_That_Is_Not_UTF8_In_The_Fallback()
	{
		// Arrange
		byte[] contents = [0xC0, 0xC1, 0xC2];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = [.. contents],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.Document.Text
			.Should()
			.Be(FileTextCodec.Fallback.GetString(contents));

		sut.IsEditingEnabled
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: a file in UTF-16 or UTF-32 is read by its byte order mark,
	/// which stays out of the text.
	/// </summary>
	[AvaloniaTest]
	[TestCase(new byte[] { 0xFF, 0xFE, 0x48, 0x00, 0x69, 0x00 })]
	[TestCase(new byte[] { 0xFE, 0xFF, 0x00, 0x48, 0x00, 0x69 })]
	[TestCase(new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00, 0x69, 0x00, 0x00, 0x00 })]
	public async Task EditorLoaded_Reads_The_Text_In_Its_Unicode_Encoding(byte[] contents)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = [.. contents],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.Document.Text
			.Should()
			.Be("Hi");
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: the text is read in the encoding chosen for the file, while
	/// the default stays the one found from the contents.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Reads_The_Text_In_The_Stored_Encoding()
	{
		// Arrange
		const string text = "Привет, мир";

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = CodePagesEncodingProvider.Instance.GetEncoding(866)!.GetBytes(text),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			FileEditorState state = new()
			{
				Encoding = "cp866"
			};

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(new SystemTextJsonSerializer().Serialize(state));

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.Document.Text
			.Should()
			.Be(text);

		sut.Editor.Encoding
			.Should()
			.Be("cp866");

		sut.Editor.DefaultEncoding
			.Should()
			.Be(FileTextCodec.Fallback.WebName);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: the stored bookmarks become part of the view state.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Restores_The_Bookmarks()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			FileEditorState state = new()
			{
				Bookmarks = [3, 7],
				FontSize = 14.0
			};

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(new SystemTextJsonSerializer().Serialize(state));

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		// A local keeps the assertion from being skipped by the null-conditional operator when there is no state.
		int[]? bookmarks = sut.Editor.ViewState?.Bookmarks;

		bookmarks
			.Should()
			.Equal(3, 7);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: a stored language is restored, none keeps the language
	/// of the extension, and a language without a grammar leaves the text plain.
	/// </summary>
	[AvaloniaTest]
	[TestCase(null, "powershell")]
	[TestCase("bat", "bat")]
	[TestCase(FileEditorState.PlainTextLanguage, null)]
	[TestCase("unknown", null)]
	public async Task EditorLoaded_Restores_The_Chosen_Syntax_Language(string? stored, string? expected)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			FileEditorState state = new()
			{
				FontSize = 14.0,
				SyntaxLanguage = stored
			};

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(new SystemTextJsonSerializer().Serialize(state));

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterInstance(SyntaxRegistry.Instance)
				.As<ISyntaxRegistry>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(fileName: "script.ps1"));

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.SyntaxLanguage
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: the stored folded or unfolded blocks become part of the view
	/// state.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Restores_The_Folded_Blocks([Values] bool isMostlyFolded)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			FileEditorState state = new()
			{
				FoldedBlocks = isMostlyFolded ? null : [3, 9],
				FontSize = 14.0,
				UnfoldedBlocks = isMostlyFolded ? [3, 9] : null
			};

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(new SystemTextJsonSerializer().Serialize(state));

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		DocumentViewState? view = sut.Editor.ViewState;

		int[]? kept = isMostlyFolded ? view?.UnfoldedBlocks : view?.FoldedBlocks;

		int[]? left = isMostlyFolded ? view?.FoldedBlocks : view?.UnfoldedBlocks;

		kept
			.Should()
			.Equal(3, 9);

		left
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: each stored switch of invisible characters
	/// reaches its own property.
	/// </summary>
	[AvaloniaTest]
	[TestCase(true, false, false)]
	[TestCase(false, true, false)]
	[TestCase(false, false, true)]
	public async Task EditorLoaded_Restores_The_Invisible_Character_Switches(
		bool showEndOfLine,
		bool showSpaces,
		bool showTabs)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			FileEditorState state = new()
			{
				CaretPosition = default,
				FontSize = 14.0,
				ScrollOffset = default,
				SelectionLength = default,
				SelectionStart = default,
				ShowEndOfLine = showEndOfLine,
				ShowSpaces = showSpaces,
				ShowTabs = showTabs,
				WordWrap = default
			};

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(new SystemTextJsonSerializer().Serialize(state));

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.ShowEndOfLine
			.Should()
			.Be(showEndOfLine);

		sut.Editor.ShowSpaces
			.Should()
			.Be(showSpaces);

		sut.Editor.ShowTabs
			.Should()
			.Be(showTabs);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: the split the file keeps for the session comes back,
	/// also without a stored editor state.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Restores_The_Split()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			IEditorSplitStore editorSplits = Substitute.For<IEditorSplitStore>();

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			editorSplits
				.Find(fileId)
				.Returns(0.25);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(editorSplits);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(fileId: fileId));

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.IsSplit
			.Should()
			.BeTrue();

		sut.Editor.SplitShare
			.Should()
			.Be(0.25);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: the stored caret, selection and scroll position
	/// become the view state.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Restores_The_View_State()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			FileEditorState state = new()
			{
				CaretPosition = new()
				{
					Column = 2,
					Line = 3,
					VisualColumn = -1
				},
				FontSize = 14.0,
				ScrollOffset = new(15, 480),
				SelectionLength = 4,
				SelectionStart = 20,
				WordWrap = false
			};

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(new SystemTextJsonSerializer().Serialize(state));

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.ViewState
			.Should()
			.Be(new DocumentViewState
			{
				CaretPosition = new(line: 3, column: 2),
				ScrollOffset = new(15.0, 480.0),
				SelectionLength = 4,
				SelectionStart = 20
			});
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: once the file is loaded, an edit is saved after a pause in
	/// typing, not at once.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Saves_An_Edit_After_A_Pause_In_Typing()
	{
		// Arrange
		byte[]? saved = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = Encoding.UTF8.GetBytes("Hello"),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(true);

			// An encrypted file hands its plain text to the cipher; the copy survives the wipe after the save.
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(x => x.ArgAt<byte[]>(2));

			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Do<byte[]>(x => saved = [.. x]))
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder.RegisterInstance(contentCipher);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: Guid.NewGuid()));

		await sut.EditorLoaded();

		// Act
		sut.Editor.Document.Text = "Hello, world";

		// A wait well shorter than the pause, which a save waits for.
		await RunJobsAsync(repeats: 10);

		byte[]? savedAtOnce = saved;

		await RunJobsUntilAsync(() => saved is not null);

		// Assert
		savedAtOnce
			.Should()
			.BeNull();

		saved
			.Should()
			.Equal(Encoding.UTF8.GetBytes("Hello, world"));
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: the editor starts from the state it is given, which the file
	/// keeps from an earlier editor, and leaves the stored one unread.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Starts_From_The_Given_State()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(
			initialEditorState: new SystemTextJsonSerializer().Serialize(new FileEditorState
			{
				WordWrap = true
			})));

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.WordWrap
			.Should()
			.BeTrue();

		await dbAccess
			.DidNotReceive()
			.GetFileEditorStateAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: the byte order mark stays out of the text, so it neither
	/// counts as a character nor shifts the first line.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Takes_The_Byte_Order_Mark_Off_The_Text()
	{
		// Arrange
		string text = RandomString.Create(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = [.. Encoding.UTF8.GetPreamble(), .. TextDefaults.Encoding.GetBytes(text)],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.Document.Text
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: the extension of the file name gives the language
	/// that the text takes by default.
	/// </summary>
	[AvaloniaTest]
	[TestCase("script.ps1", "powershell")]
	[TestCase("notes.txt", null)]
	public async Task EditorLoaded_Takes_The_Default_Syntax_Language_From_The_File_Name(string fileName, string? expected)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterInstance(SyntaxRegistry.Instance)
				.As<ISyntaxRegistry>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(fileName: fileName));

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.DefaultSyntaxLanguage
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: an encoding chosen for the file that cannot read its contents
	/// gives way to the one found from them: here UTF-16 for an odd number of bytes.
	/// </summary>
	[AvaloniaTest]
	public async Task EditorLoaded_Takes_The_Found_Encoding_When_The_Stored_One_Cannot_Read()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = Encoding.UTF8.GetBytes("Hi!"),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			FileEditorState state = new()
			{
				Encoding = "utf-16"
			};

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(new SystemTextJsonSerializer().Serialize(state));

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.Document.Text
			.Should()
			.Be("Hi!");

		sut.Editor.Encoding
			.Should()
			.Be(Encoding.UTF8.WebName);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.EditorLoaded" />: the extension of the file name gives the language of the text.
	/// </summary>
	[AvaloniaTest]
	[TestCase("script.ps1", "powershell")]
	[TestCase("notes.txt", null)]
	[TestCase(null, null)]
	public async Task EditorLoaded_Takes_The_Syntax_Language_From_The_File_Name(string? fileName, string? expected)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterInstance(SyntaxRegistry.Instance)
				.As<ISyntaxRegistry>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(fileName: fileName));

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Editor.SyntaxLanguage
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.Encoding" />: an edit made before another encoding is chosen reaches the file
	/// in the bytes it was typed in, and the text is read again from them.
	/// </summary>
	[AvaloniaTest]
	public async Task Encoding_Keeps_The_Edit_Made_Before_It()
	{
		// Arrange
		byte[]? saved = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = Encoding.UTF8.GetBytes("Hello"),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(true);

			// An encrypted file hands its plain text to the cipher; the copy survives the wipe after the save.
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(x => x.ArgAt<byte[]>(2));

			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Do<byte[]>(x => saved = [.. x]))
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<DocumentEncodingSwitcher>()
				.As<IDocumentEncodingSwitcher>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder.RegisterInstance(contentCipher);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: Guid.NewGuid()));

		await sut.EditorLoaded();

		sut.Editor.Document.Text = "Привет";

		byte[] expected = Encoding.UTF8.GetBytes("Привет");

		string reread = CodePagesEncodingProvider.Instance.GetEncoding(1251)!.GetString(expected);

		// Act
		sut.Editor.Encoding = "windows-1251";

		await RunJobsUntilAsync(() => saved is not null && sut.Editor.Document.Text == reread);

		// Assert
		saved
			.Should()
			.Equal(expected);

		sut.Editor.Document.Text
			.Should()
			.Be(reread);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.Encoding" />: another encoding reads the text again from the same bytes, with
	/// nothing to undo.
	/// </summary>
	[AvaloniaTest]
	public async Task Encoding_Reads_The_Text_Again_In_The_Chosen_Encoding()
	{
		// Arrange
		const string text = "Привет, мир";

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				// Not UTF-8, so read in the fallback code page first
				Contents = CodePagesEncodingProvider.Instance.GetEncoding(866)!.GetBytes(text),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<DocumentEncodingSwitcher>()
				.As<IDocumentEncodingSwitcher>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		await sut.EditorLoaded();

		// Act
		sut.Editor.Encoding = "cp866";

		// Assert
		sut.Editor.Document.Text
			.Should()
			.Be(text);

		sut.Editor.Document.UndoStack.CanUndo
			.Should()
			.BeFalse();

		sut.Editor.EncodingName
			.Should()
			.Be("CP866");
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.Encoding" />: the text read again in another encoding comes in the document the
	/// view already shows.
	/// </summary>
	[AvaloniaTest]
	public async Task Encoding_Reads_The_Text_Again_Into_The_Same_Document()
	{
		// Arrange
		const string text = "Привет, мир";

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				// Not UTF-8, so read in the fallback code page first
				Contents = CodePagesEncodingProvider.Instance.GetEncoding(866)!.GetBytes(text),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<DocumentEncodingSwitcher>()
				.As<IDocumentEncodingSwitcher>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		await sut.EditorLoaded();

		TextDocument document = sut.Editor.Document;

		// Act
		sut.Editor.Encoding = "cp866";

		// Assert
		document.Text
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.Encoding" />: the encoding found from the contents is stored as no choice.
	/// </summary>
	[AvaloniaTest]
	public async Task Encoding_Saves_No_Choice_For_The_Encoding_Of_The_Contents()
	{
		// Arrange
		string? reported = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = Encoding.UTF8.GetBytes("Hello"),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			FileEditorState state = new()
			{
				Encoding = "windows-1251"
			};

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(new SystemTextJsonSerializer().Serialize(state));

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<DocumentEncodingSwitcher>()
				.As<IDocumentEncodingSwitcher>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		await sut.EditorLoaded();

		// Act
		sut.Editor.Encoding = Encoding.UTF8.WebName;

		// Assert
		string? stored = new SystemTextJsonSerializer().Deserialize<FileEditorState>(reported!).Encoding;

		stored
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.Encoding" />: an edit is not saved before the text is read again: it waits for
	/// its pause in typing, as any edit does.
	/// </summary>
	[AvaloniaTest]
	public async Task Encoding_Saves_Nothing_Before_Reading_The_Text_Again()
	{
		// Arrange
		byte[]? saved = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = Encoding.UTF8.GetBytes("Hello"),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(true);

			// An encrypted file hands its plain text to the cipher; the copy survives the wipe after the save.
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(x => x.ArgAt<byte[]>(2));

			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Do<byte[]>(x => saved = [.. x]))
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<DocumentEncodingSwitcher>()
				.As<IDocumentEncodingSwitcher>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder.RegisterInstance(contentCipher);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: Guid.NewGuid()));

		await sut.EditorLoaded();

		sut.Editor.Document.Text = "Hello, world";

		// Act
		sut.Editor.Encoding = "windows-1251";

		// Assert
		saved
			.Should()
			.BeNull();

		sut.Editor.Document.Text
			.Should()
			.Be("Hello, world");
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.Encoding" />: an encoding other than the one found from the contents is stored
	/// in the editor state.
	/// </summary>
	[AvaloniaTest]
	public async Task Encoding_Saves_The_Chosen_Encoding()
	{
		// Arrange
		string? reported = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = Encoding.UTF8.GetBytes("Hello"),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<DocumentEncodingSwitcher>()
				.As<IDocumentEncodingSwitcher>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		await sut.EditorLoaded();

		// Act
		sut.Editor.Encoding = "cp866";

		// Assert
		string? stored = new SystemTextJsonSerializer().Deserialize<FileEditorState>(reported!).Encoding;

		stored
			.Should()
			.Be("cp866");
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.Encoding" />: an encoding that cannot read the text is refused with a warning in
	/// the main window, which shows the editor: here UTF-16 for an odd number of bytes.
	/// </summary>
	[AvaloniaTest]
	public async Task Encoding_Tells_In_The_Main_Window_Why_The_Choice_Cannot_Read_The_Text()
	{
		// Arrange
		INotificationService notification = Substitute.For<INotificationService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = Encoding.UTF8.GetBytes("Hi!"),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<DocumentEncodingSwitcher>()
				.As<IDocumentEncodingSwitcher>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(notification);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		await sut.EditorLoaded();

		// Act
		sut.Editor.Encoding = Encoding.Unicode.WebName;

		// Assert
		notification
			.Received(1)
			.ShowWarningSnackbar(Arg.Any<string>(), Arg.Is(SnackbarHostIdentifiers.Main));
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.FontSize" />: a new font size saves the editor state with it.
	/// </summary>
	[AvaloniaTest]
	public async Task FontSize_Saves_The_Editor_State()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		string? reported = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		await sut.EditorLoaded();

		// Act
		sut.Editor.FontSize = 20.0;

		// Assert
		double stored = new SystemTextJsonSerializer().Deserialize<FileEditorState>(reported!).FontSize;

		stored
			.Should()
			.Be(20.0);

		await dbAccess
			.ReceivedWithAnyArgs(1)
			.UpdateFileEditorStateAsync(default, default!, default);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ShowEndOfLine" />, <see cref="DocumentEditorViewModel.ShowSpaces" />
	/// and <see cref="DocumentEditorViewModel.ShowTabs" />: each switch turned on saves the editor state with it.
	/// </summary>
	[AvaloniaTest]
	[TestCase(true, false, false)]
	[TestCase(false, true, false)]
	[TestCase(false, false, true)]
	public async Task Invisible_Character_Switches_Save_The_Editor_State(
		bool showEndOfLine,
		bool showSpaces,
		bool showTabs)
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		string? reported = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		await sut.EditorLoaded();

		string expected = new SystemTextJsonSerializer().Serialize(
			new FileEditorState
			{
				CaretPosition = default,
				FontSize = sut.Editor.FontSize,
				ScrollOffset = default,
				SelectionLength = default,
				SelectionStart = default,
				ShowEndOfLine = showEndOfLine,
				ShowSpaces = showSpaces,
				ShowTabs = showTabs,
				WordWrap = sut.Editor.WordWrap
			},
			JsonDefaults.Options);

		// Act
		sut.Editor.ShowEndOfLine = showEndOfLine;

		sut.Editor.ShowSpaces = showSpaces;

		sut.Editor.ShowTabs = showTabs;

		// Assert
		reported
			.Should()
			.Be(expected);

		await dbAccess
			.ReceivedWithAnyArgs(1)
			.UpdateFileEditorStateAsync(default, default!, default);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.IsSplit" />: the end of the split is kept for the file as no split.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Off_Stores_No_Split()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		IEditorSplitStore editorSplits = Substitute.For<IEditorSplitStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(editorSplits));

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(fileId: fileId));

		sut.Editor.IsSplit = true;

		// Act
		sut.Editor.IsSplit = false;

		// Assert
		editorSplits
			.Received(1)
			.Set(fileId, null);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.IsSplit" />: the split keeps the share of the upper half for the file.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Stores_The_Share_Of_The_Upper_Half()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		IEditorSplitStore editorSplits = Substitute.For<IEditorSplitStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(editorSplits));

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(fileId: fileId));

		sut.Editor.SplitShare = 0.25;

		// Act
		sut.Editor.IsSplit = true;

		// Assert
		editorSplits
			.Received(1)
			.Set(fileId, 0.25);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: an edit the keeper refuses to encrypt is not
	/// written, and the flush says so, so the contents are not hidden over the edit.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_Flush_Answers_False_When_The_Encryption_Is_Refused()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		List<Task> watched = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			// The session key opens the file and then refuses the edit.
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(x => x.ArgAt<byte[]>(2));

			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns((byte[]?)null);

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler.Watch(Arg.Do<Task>(watched.Add));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(exceptionHandler);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: Guid.NewGuid()));

		await sut.EditorLoaded();

		sut.Editor.Document.Text = RandomString.Create(10);

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		IReadOnlyCollection<bool> responses = await flush.GetResponsesAsync();

		// Completing the save channel lets its consumer run to the end.
		sut.Dispose();

		await Task.WhenAll(watched);

		// Assert
		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFileContentsAsync(default, default!, default, default);

		responses
			.Should()
			.Equal(false);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: an edit that finds no file in the database
	/// stays unsaved, and the flush says so.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_Flush_Answers_False_When_The_File_Is_Not_In_The_Database()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			// No row takes the edit, as the file is gone from the database.
			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(false);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		await sut.EditorLoaded();

		sut.Editor.Document.Text = RandomString.Create(10);

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		IReadOnlyCollection<bool> responses = await flush.GetResponsesAsync();

		// Assert
		responses
			.Should()
			.Equal(false);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: in read-only mode a flush leaves
	/// a change made in the mode unsaved, as the mode saves nothing after it turned on.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_Flush_In_Read_Only_Mode_Leaves_A_Change_Made_In_It_Unsaved()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		await sut.EditorLoaded();

		sut.Receive(new EditorReadOnlyModeChangedMessage(true));

		sut.Editor.Document.Text = RandomString.Create(10);

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		await flush.GetResponsesAsync();

		// Assert
		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFileContentsAsync(default, default!, default, default);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: in read-only mode a flush answers
	/// only once the edit queued as the mode turned on is saved, so the contents are not hidden before it.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_Flush_In_Read_Only_Mode_Waits_For_The_Queued_Save()
	{
		// Arrange
		TaskCompletionSource<bool> written = new(TaskCreationOptions.RunContinuationsAsynchronously);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			// The save of the edit lasts until the test lets it end.
			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(written.Task);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		await sut.EditorLoaded();

		sut.Editor.Document.Text = RandomString.Create(10);

		sut.Receive(new EditorReadOnlyModeChangedMessage(true));

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		Task<IReadOnlyCollection<bool>> answer = flush.GetResponsesAsync();

		bool isAnsweredBeforeTheSave = answer.IsCompleted;

		written.SetResult(true);

		IReadOnlyCollection<bool> responses = await answer;

		// Assert
		isAnsweredBeforeTheSave
			.Should()
			.BeFalse();

		responses
			.Should()
			.Equal(true);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: a text with a character that the encoding of
	/// the file does not have is not saved, and the flush says so, so the contents are not hidden over the edit.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_Flush_Keeps_A_Text_Outside_The_Encoding_Unsaved()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		List<Task> watched = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents fileContents = new()
			{
				// Not UTF-8, so in the fallback code page, which has no emoji
				Contents = [0xC0, 0xC1, 0xC2],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler.Watch(Arg.Do<Task>(watched.Add));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder.RegisterInstance(exceptionHandler);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		await sut.EditorLoaded();

		sut.Editor.Document.Text += "😀";

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		IReadOnlyCollection<bool> responses = await flush.GetResponsesAsync();

		// Completing the save channel lets its consumer run to the end.
		sut.Dispose();

		await Task.WhenAll(watched);

		// Assert
		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFileContentsAsync(default, default!, default, default);

		responses
			.Should()
			.Equal(false);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: a flush of a file nobody edited writes nothing,
	/// also when the file starts with a byte order mark.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_Flush_Leaves_An_Unedited_File_Alone([Values] bool hasByteOrderMark)
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		List<Task> watched = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] mark = hasByteOrderMark ? Encoding.UTF8.GetPreamble() : [];

			ValidatedContents fileContents = new()
			{
				Contents = [.. mark, .. TextDefaults.Encoding.GetBytes(RandomString.Create(10))],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler.Watch(Arg.Do<Task>(watched.Add));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder.RegisterInstance(exceptionHandler);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		await sut.EditorLoaded();

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		IReadOnlyCollection<bool> responses = await flush.GetResponsesAsync();

		// Completing the save channel lets its consumer run to the end.
		sut.Dispose();

		await Task.WhenAll(watched);

		// Assert
		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFileContentsAsync(default, default!, default, default);

		responses
			.Should()
			.Equal(true);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: a flush writes nothing over contents
	/// that are not text, so the file keeps its bytes.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_Flush_Leaves_Non_Text_Contents_Untouched()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		List<Task> watched = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents fileContents = new()
			{
				// Start of a PNG image: its signature and the length of its first chunk
				Contents = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler.Watch(Arg.Do<Task>(watched.Add));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder.RegisterInstance(exceptionHandler);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		await sut.EditorLoaded();

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		IReadOnlyCollection<bool> responses = await flush.GetResponsesAsync();

		// Completing the save channel lets its consumer run to the end.
		sut.Dispose();

		await Task.WhenAll(watched);

		// Assert
		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFileContentsAsync(default, default!, default, default);

		responses
			.Should()
			.Equal(true);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: a saved edit reports the date of change it is
	/// written with.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_Flush_Reports_The_Date_Of_Change_Of_The_Edit()
	{
		// Arrange
		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(true);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		DateTime? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setUpdatedAtCallback: x => reported = x));

		await sut.EditorLoaded();

		sut.Editor.Document.Text = RandomString.Create(10);

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		await flush.GetResponsesAsync();

		// Assert
		reported
			.Should()
			.Be(time.GetLocalNow().LocalDateTime);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: once the character that the encoding of the
	/// file does not have is gone, the text is saved again.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_Flush_Saves_A_Text_That_Fits_The_Encoding_Again()
	{
		// Arrange
		byte[]? saved = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				// Not UTF-8, so in the fallback code page, which has no emoji
				Contents = [0xC0, 0xC1, 0xC2],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(true);

			// An encrypted file hands its plain text to the cipher; the copy survives the wipe after the save.
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(x => x.ArgAt<byte[]>(2));

			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Do<byte[]>(x => saved = [.. x]))
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder.RegisterInstance(contentCipher);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: Guid.NewGuid()));

		await sut.EditorLoaded();

		string text = sut.Editor.Document.Text;

		sut.Editor.Document.Text = $"{text}😀";

		FlushEditorsMessage refused = new();

		sut.Receive(refused);

		await refused.GetResponsesAsync();

		sut.Editor.Document.Text = $"{text}x";

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		IReadOnlyCollection<bool> responses = await flush.GetResponsesAsync();

		// Assert
		byte[] expected = [0xC0, 0xC1, 0xC2, 0x78];

		saved
			.Should()
			.Equal(expected);

		responses
			.Should()
			.Equal(true);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: an edited file is saved with the byte order mark
	/// it was loaded with, and without one when it had none.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_Flush_Saves_The_Byte_Order_Mark_It_Loaded([Values] bool hasByteOrderMark)
	{
		// Arrange
		string text = RandomString.Create(10);

		byte[] mark = hasByteOrderMark ? Encoding.UTF8.GetPreamble() : [];

		byte[]? saved = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = [.. mark, .. TextDefaults.Encoding.GetBytes(RandomString.Create(10))],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(true);

			// An encrypted file hands its plain text to the cipher; the copy survives the wipe after the save.
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(x => x.ArgAt<byte[]>(2));

			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Do<byte[]>(x => saved = [.. x]))
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder.RegisterInstance(contentCipher);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: Guid.NewGuid()));

		await sut.EditorLoaded();

		sut.Editor.Document.Text = text;

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		IReadOnlyCollection<bool> responses = await flush.GetResponsesAsync();

		// Assert
		byte[] expected = [.. mark, .. TextDefaults.Encoding.GetBytes(text)];

		saved
			.Should()
			.Equal(expected);

		responses
			.Should()
			.Equal(true);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: a save that throws leaves the edit unsaved,
	/// and the next flush saves it.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_Flush_Saves_The_Edit_Again_After_A_Failed_Save()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(
					Task.FromException<bool>(new InvalidOperationException()),
					Task.FromResult(true));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		await sut.EditorLoaded();

		sut.Editor.Document.Text = RandomString.Create(10);

		FlushEditorsMessage failedFlush = new();

		sut.Receive(failedFlush);

		IReadOnlyCollection<bool> failedResponses = await failedFlush.GetResponsesAsync();

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		IReadOnlyCollection<bool> responses = await flush.GetResponsesAsync();

		// Assert
		failedResponses
			.Should()
			.Equal(false);

		responses
			.Should()
			.Equal(true);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: a flush saves the document text as it is,
	/// including an edit the text change handler has not queued yet.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_Flush_Saves_The_Latest_Document_Text()
	{
		// Arrange
		string text = RandomString.Create(10);

		byte[]? saved = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(true);

			// An encrypted file hands its plain text to the cipher; the copy survives the wipe after the save.
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(x => x.ArgAt<byte[]>(2));

			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Do<byte[]>(x => saved = [.. x]))
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder.RegisterInstance(contentCipher);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: Guid.NewGuid()));

		await sut.EditorLoaded();

		sut.Editor.Document.Text = text;

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		IReadOnlyCollection<bool> responses = await flush.GetResponsesAsync();

		// Assert
		saved
			.Should()
			.Equal(TextDefaults.Encoding.GetBytes(text));

		responses
			.Should()
			.Equal(true);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: an edited file is saved in the encoding it was
	/// loaded in, with its byte order mark, and a file that is not UTF-8 in the fallback code page.
	/// Rarely it fails before its body runs, when a thread of an earlier test takes the new UI dispatcher of Avalonia.
	/// </summary>
	[AvaloniaTest]
	[TestCaseSource(nameof(TypedEncodedContents))]
	public async Task Receive_Flush_Saves_The_Text_In_The_Encoding_It_Loaded(byte[] contents, byte[] expected)
	{
		// Arrange
		byte[]? saved = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = [.. contents],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			dbAccess
				.UpdateFileContentsAsync(default, default!, default, default)
				.ReturnsForAnyArgs(true);

			// An encrypted file hands its plain text to the cipher; the copy survives the wipe after the save.
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(x => x.ArgAt<byte[]>(2));

			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Do<byte[]>(x => saved = [.. x]))
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder.RegisterInstance(contentCipher);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: Guid.NewGuid()));

		await sut.EditorLoaded();

		sut.Editor.Document.Text += "x";

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		IReadOnlyCollection<bool> responses = await flush.GetResponsesAsync();

		// Assert
		saved
			.Should()
			.Equal(expected);

		responses
			.Should()
			.Equal(true);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(FlushEditorsMessage)" />: a flush that arrives while the contents
	/// are loading queues nothing, so the still empty editor never overwrites the file.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_Flush_While_Loading_Writes_Nothing()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		TaskCompletionSource<ValidatedContents> read = new();

		List<Task> watched = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(read.Task);

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler.Watch(Arg.Do<Task>(watched.Add));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder.RegisterInstance(exceptionHandler);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		Task loading = sut.EditorLoaded();

		FlushEditorsMessage flush = new();

		// Act
		sut.Receive(flush);

		IReadOnlyCollection<bool> responses = await flush.GetResponsesAsync();

		read.SetResult(new()
		{
			Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
			IsValid = true
		});

		await loading;

		// Completing the save channel lets its consumer run to the end.
		sut.Dispose();

		await Task.WhenAll(watched);

		// Assert
		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFileContentsAsync(default, default!, default, default);

		responses
			.Should()
			.Equal(true);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(EditorReadOnlyModeChangedMessage)" />: an edit made just
	/// before the read-only mode, still waiting for a pause in typing, is saved as the mode turns on.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_ReadOnly_Saves_The_Edit_Made_Before_It()
	{
		// Arrange
		string text = RandomString.Create(10);

		byte[]? saved = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			// An encrypted file hands its plain text to the cipher; the copy survives the wipe after the save.
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(x => x.ArgAt<byte[]>(2));

			contentCipher
				.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Do<byte[]>(x => saved = [.. x]))
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();

			builder.RegisterInstance(contentCipher);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: Guid.NewGuid()));

		await sut.EditorLoaded();

		sut.Editor.Document.Text = text;

		// Act
		sut.Receive(new EditorReadOnlyModeChangedMessage(true));

		// A flush in read-only mode waits for the save queued as the mode turned on.
		FlushEditorsMessage flush = new();

		sut.Receive(flush);

		await flush.GetResponsesAsync();

		// Assert
		saved
			.Should()
			.Equal(TextDefaults.Encoding.GetBytes(text));
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.Receive(EditorReadOnlyModeChangedMessage)" />: the read-only mode
	/// turned on without an edit writes nothing.
	/// </summary>
	[AvaloniaTest]
	public async Task Receive_ReadOnly_Writes_Nothing_Without_An_Edit()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		await sut.EditorLoaded();

		// Act
		sut.Receive(new EditorReadOnlyModeChangedMessage(true));

		// A flush in read-only mode waits for the save queued as the mode turned on.
		FlushEditorsMessage flush = new();

		sut.Receive(flush);

		await flush.GetResponsesAsync();

		// Assert
		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFileContentsAsync(default, default!, default, default);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.ShowInListCommand" />: the editor asks to show its file in the tree.
	/// </summary>
	[AvaloniaTest]
	public void ShowInListCommand_Asks_To_Show_The_File_In_The_Tree()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		StrongReferenceMessenger messenger = new();

		ShowInEditorMessage? sent = null;

		messenger.Register<ShowInEditorMessage>(this, (_, message) => sent = message);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<IMessenger>(messenger));

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(fileId: fileId));

		// Act
		sut.ShowInListCommand.Execute(null);

		// Assert
		sent!.Id
			.Should()
			.Be(fileId);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.SplitShare" />: a new share of a split document is kept for the file.
	/// </summary>
	[AvaloniaTest]
	public void SplitShare_Stores_The_New_Share_While_Split()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		IEditorSplitStore editorSplits = Substitute.For<IEditorSplitStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(editorSplits));

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(fileId: fileId));

		sut.Editor.IsSplit = true;

		// Act
		sut.Editor.SplitShare = 0.25;

		// Assert
		editorSplits
			.Received(1)
			.Set(fileId, 0.25);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.SyntaxLanguage" />: a return to the language of the extension stores no choice.
	/// </summary>
	[AvaloniaTest]
	[TestCase("script.ps1", "powershell")]
	[TestCase("notes.txt", null)]
	public async Task SyntaxLanguage_Saves_No_Choice_For_The_Language_Of_The_Extension(string fileName, string? defaultLanguage)
	{
		// Arrange
		string? reported = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterInstance(SyntaxRegistry.Instance)
				.As<ISyntaxRegistry>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(
			fileName: fileName,
			setEditorStateCallback: x => reported = x));

		await sut.EditorLoaded();

		sut.Editor.SyntaxLanguage = "bat";

		// Act
		sut.Editor.SyntaxLanguage = defaultLanguage;

		// Assert
		string? stored = new SystemTextJsonSerializer().Deserialize<FileEditorState>(reported!).SyntaxLanguage;

		stored
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.SyntaxLanguage" />: a language other than the one of the extension is stored
	/// in the editor state, plain text under an id of its own.
	/// </summary>
	[AvaloniaTest]
	[TestCase("script.ps1", "bat", "bat")]
	[TestCase("script.ps1", null, FileEditorState.PlainTextLanguage)]
	[TestCase("notes.txt", "bat", "bat")]
	public async Task SyntaxLanguage_Saves_The_Chosen_Language(string fileName, string? chosen, string expected)
	{
		// Arrange
		string? reported = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterInstance(SyntaxRegistry.Instance)
				.As<ISyntaxRegistry>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(
			fileName: fileName,
			setEditorStateCallback: x => reported = x));

		await sut.EditorLoaded();

		// Act
		sut.Editor.SyntaxLanguage = chosen;

		// Assert
		string? stored = new SystemTextJsonSerializer().Deserialize<FileEditorState>(reported!).SyntaxLanguage;

		stored
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ViewState" />: the folded and unfolded blocks of a protected file stay out of
	/// the editor state, as they would give away the outline of its text, while its bookmarks are saved.
	/// </summary>
	[AvaloniaTest]
	public async Task ViewState_Saves_No_Folded_Blocks_Of_An_Encrypted_File()
	{
		// Arrange
		string? reported = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			// A protected file hands its contents to the cipher, which gives them back as they are.
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(x => x.ArgAt<byte[]>(2));

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder.RegisterInstance(contentCipher);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(
			keeperId: Guid.NewGuid(),
			setEditorStateCallback: x => reported = x));

		await sut.EditorLoaded();

		// Act
		sut.Editor.ViewState = new DocumentViewState
		{
			Bookmarks = [2],
			CaretPosition = new(line: 1, column: 1),
			FoldedBlocks = [10, 40],
			ScrollOffset = default,
			SelectionLength = 0,
			SelectionStart = 0,
			UnfoldedBlocks = [70]
		};

		// Assert
		FileEditorState state = new SystemTextJsonSerializer().Deserialize<FileEditorState>(reported!);

		state.Bookmarks
			.Should()
			.Equal(2);

		state.FoldedBlocks
			.Should()
			.BeNull();

		state.UnfoldedBlocks
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ViewState" />: a view state reported after the editor has been closed
	/// is not saved.
	/// </summary>
	[AvaloniaTest]
	public async Task ViewState_Saves_Nothing_After_Dispose()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		await sut.EditorLoaded();

		sut.Dispose();

		// Act
		sut.Editor.ViewState = new DocumentViewState
		{
			CaretPosition = new(line: 3, column: 2),
			ScrollOffset = new(15.0, 480.0),
			SelectionLength = 4,
			SelectionStart = 20
		};

		// Assert
		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFileEditorStateAsync(default, default!, default);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ViewState" />: the bookmarks of a new view state go into the editor state.
	/// </summary>
	[AvaloniaTest]
	public async Task ViewState_Saves_The_Bookmarks()
	{
		// Arrange
		string? reported = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		await sut.EditorLoaded();

		// Act
		sut.Editor.ViewState = new DocumentViewState
		{
			Bookmarks = [2, 5],
			CaretPosition = new(line: 1, column: 1),
			ScrollOffset = default,
			SelectionLength = 0,
			SelectionStart = 0
		};

		// Assert
		new SystemTextJsonSerializer().Deserialize<FileEditorState>(reported!).Bookmarks
			.Should()
			.Equal(2, 5);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ViewState" />: a new view state is saved together with the font size
	/// and the word wrap.
	/// </summary>
	[AvaloniaTest]
	public async Task ViewState_Saves_The_Editor_State()
	{
		// Arrange
		using TestDatabase database = new();

		FileEntity file = EntityFactory.CreateFile(contents: TextDefaults.Encoding.GetBytes(RandomString.Create(10)));

		string? reported = null;

		List<Task> watched = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler.Watch(Arg.Do<Task>(watched.Add));

			builder.RegisterInstance(database.Context);

			builder.RegisterInstance(exceptionHandler);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFilesAsync([file]);

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(
			fileId: file.Id,
			setEditorStateCallback: x => reported = x));

		await sut.EditorLoaded();

		string expected = new SystemTextJsonSerializer().Serialize(
			new FileEditorState
			{
				CaretPosition = new()
				{
					Column = 2,
					Line = 3,
					VisualColumn = -1
				},
				FontSize = sut.Editor.FontSize,
				ScrollOffset = new(15, 480),
				SelectionLength = 4,
				SelectionStart = 20,
				WordWrap = sut.Editor.WordWrap
			},
			JsonDefaults.Options);

		int watchedBefore = watched.Count;

		// Act
		sut.Editor.ViewState = new DocumentViewState
		{
			CaretPosition = new(line: 3, column: 2),
			ScrollOffset = new(15.0, 480.0),
			SelectionLength = 4,
			SelectionStart = 20
		};

		// The save of the editor state is the next task handed over.
		await watched[watchedBefore];

		// Assert
		reported
			.Should()
			.Be(expected);

		FileEntity[] stored = await dbAccess.GetAllFilesAsync(OptionalFileProperties.EditorState);

		stored
			.Should()
			.ContainSingle()
			.Which
			.EditorState
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ViewState" />: the folded and unfolded blocks of a new view state go into the
	/// editor state.
	/// </summary>
	[AvaloniaTest]
	public async Task ViewState_Saves_The_Folded_Blocks()
	{
		// Arrange
		string? reported = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setEditorStateCallback: x => reported = x));

		await sut.EditorLoaded();

		// Act
		sut.Editor.ViewState = new DocumentViewState
		{
			CaretPosition = new(line: 1, column: 1),
			FoldedBlocks = [10, 40],
			ScrollOffset = default,
			SelectionLength = 0,
			SelectionStart = 0,
			UnfoldedBlocks = [70]
		};

		// Assert
		FileEditorState state = new SystemTextJsonSerializer().Deserialize<FileEditorState>(reported!);

		state.FoldedBlocks
			.Should()
			.Equal(10, 40);

		state.UnfoldedBlocks
			.Should()
			.Equal(70);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.WordWrap" />: a change of an editor setting saves the editor state and leaves
	/// the date of change of the file alone.
	/// </summary>
	[AvaloniaTest]
	public async Task WordWrap_Keeps_The_Date_Of_Change()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(RandomString.Create(10)),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsWriter>()
				.As<IFileContentsWriter>();

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		DateTime? reported = null;

		sut.Initialize(EmbeddedEditorSetupFactory.CreateSetup(setUpdatedAtCallback: x => reported = x));

		await sut.EditorLoaded();

		// Act
		sut.Editor.WordWrap = !sut.Editor.WordWrap;

		// Assert
		reported
			.Should()
			.BeNull();

		await dbAccess
			.ReceivedWithAnyArgs(1)
			.UpdateFileEditorStateAsync(default, default!, default);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// File contents that are not text.
	/// </summary>
	private static byte[][] NonTextContents() =>
	[
		// UTF-32 without a byte order mark: valid UTF-8, but with zero bytes that UTF-16 does not explain
		[0x48, 0x00, 0x00, 0x00, 0x69, 0x00, 0x00, 0x00],
		// Start of a PNG image: its signature and the length of its first chunk
		[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D],
		// Bytes that are not UTF-8, with control characters that no text holds
		[0xC9, 0x01, 0x02, 0x10]
	];

	/// <summary>
	/// Runs the jobs of the UI thread <paramref name="repeats" /> times, with a short wait between the runs.
	/// </summary>
	private static async Task RunJobsAsync(int repeats)
	{
		Func<bool> never = static () =>
		{
			Dispatcher.UIThread.RunJobs();

			return false;
		};

		await never.WaitAsync(millisecondsDelay: 10, maxRepeats: repeats);
	}

	/// <summary>
	/// Runs the jobs of the UI thread until <paramref name="condition" /> holds or the wait runs out.
	/// </summary>
	private static async Task RunJobsUntilAsync(Func<bool> condition)
	{
		Func<bool> isMet = () =>
		{
			Dispatcher.UIThread.RunJobs();

			return condition();
		};

		await isMet.WaitAsync(millisecondsDelay: 10, maxRepeats: 300);
	}

	/// <summary>
	/// Contents of files in encodings other than UTF-8, each with the bytes it has after an "x" is typed at its end.
	/// </summary>
	private static byte[][][] TypedEncodedContents() =>
	[
		// UTF-16 LE with a byte order mark
		[
			[0xFF, 0xFE, 0x41, 0x00],
			[0xFF, 0xFE, 0x41, 0x00, 0x78, 0x00]
		],
		// UTF-16 BE with a byte order mark
		[
			[0xFE, 0xFF, 0x00, 0x41],
			[0xFE, 0xFF, 0x00, 0x41, 0x00, 0x78]
		],
		// UTF-16 LE without a byte order mark, which the save does not add
		[
			[0x41, 0x00],
			[0x41, 0x00, 0x78, 0x00]
		],
		// UTF-32 LE with a byte order mark
		[
			[0xFF, 0xFE, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00],
			[0xFF, 0xFE, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00, 0x78, 0x00, 0x00, 0x00]
		],
		// Not UTF-8, so in the fallback code page, where the letter keeps its ASCII byte
		[
			[0xC0, 0xC1, 0xC2],
			[0xC0, 0xC1, 0xC2, 0x78]
		]
	];
	#endregion
}
