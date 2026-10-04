using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Messages.Editor;
using DataOrganizer.ViewModels;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Common;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using TestSupport.Common;

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
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		bool? isOnUiThread = null;

		sut.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(EmbeddedFileEditorViewModel.ViewState))
			{
				isOnUiThread = Dispatcher.UIThread.CheckAccess();
			}
		};

		string json = new SystemTextJsonSerializer().Serialize(new FileEditorState
		{
			CaretPosition = new(line: 3, column: 2),
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Document.UndoStack.CanUndo
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
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.IsInitialized
			.Should()
			.BeTrue();

		sut.Document.Text
			.Should()
			.Be(TextDefaults.Encoding.GetString(contents));

		sut.WordWrap
			.Should()
			.BeTrue();

		sut.FontSize
			.Should()
			.Be(fontSize);
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.EncodingName
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);
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
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.FontSize
			.Should()
			.Be(16.5);

		sut.ShowEndOfLine
			.Should()
			.BeTrue();

		sut.ShowSpaces
			.Should()
			.BeTrue();

		sut.ShowTabs
			.Should()
			.BeTrue();

		sut.SyntaxLanguage
			.Should()
			.Be("python");

		sut.ViewState
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

		sut.WordWrap
			.Should()
			.BeTrue();

		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFilePropertiesAsync(default, default!, default);
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
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.FontSize
			.Should()
			.Be(20.0);

		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFilePropertiesAsync(default, default!, default);
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Document.Text
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Document.Text
			.Should()
			.Be("Hi");
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
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		// A local keeps the assertion from being skipped by the null-conditional operator when there is no state.
		int[]? bookmarks = sut.ViewState?.Bookmarks;

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
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.FileName = "script.ps1";

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.SyntaxLanguage
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
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		DocumentViewState? view = sut.ViewState;

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
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.ShowEndOfLine
			.Should()
			.Be(showEndOfLine);

		sut.ShowSpaces
			.Should()
			.Be(showSpaces);

		sut.ShowTabs
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
				.Returns((string?)null);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.InitialEditorSplit = 0.25;

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.IsSplit
			.Should()
			.BeTrue();

		sut.SplitShare
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
				CaretPosition = new(line: 3, column: 2),
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
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.ViewState
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.Document.Text
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.FileName = fileName;

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.DefaultSyntaxLanguage
			.Should()
			.Be(expected);
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.FileName = fileName;

		// Act
		await sut.EditorLoaded();

		// Assert
		sut.SyntaxLanguage
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.ShowEndOfLine" />, <see cref="EmbeddedFileEditorViewModel.ShowSpaces" />
	/// and <see cref="EmbeddedFileEditorViewModel.ShowTabs" />: each switch turned on saves the editor state with it.
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
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.SetEditorStateCallback = x => reported = x;

		await sut.EditorLoaded();

		string expected = new SystemTextJsonSerializer().Serialize(
			new FileEditorState
			{
				CaretPosition = default,
				FontSize = sut.FontSize,
				ScrollOffset = default,
				SelectionLength = default,
				SelectionStart = default,
				ShowEndOfLine = showEndOfLine,
				ShowSpaces = showSpaces,
				ShowTabs = showTabs,
				WordWrap = sut.WordWrap
			},
			JsonDefaults.Options);

		// Act
		sut.ShowEndOfLine = showEndOfLine;

		sut.ShowSpaces = showSpaces;

		sut.ShowTabs = showTabs;

		// Assert
		reported
			.Should()
			.Be(expected);

		await dbAccess
			.ReceivedWithAnyArgs(1)
			.UpdateFilePropertiesAsync(default, default!, default);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.IsSplit" />: the end of the split reports that there is none.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Off_Reports_No_Split()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.IsSplit = true;

		List<double?> reported = [];

		sut.SetEditorSplitCallback = reported.Add;

		// Act
		sut.IsSplit = false;

		// Assert
		reported
			.Should()
			.Equal((double?)null);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.IsSplit" />: the split reports the share of the upper half.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Reports_The_Share_Of_The_Upper_Half()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.SplitShare = 0.25;

		List<double?> reported = [];

		sut.SetEditorSplitCallback = reported.Add;

		// Act
		sut.IsSplit = true;

		// Assert
		reported
			.Should()
			.Equal(0.25);
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler.Watch(Arg.Do<Task>(watched.Add));

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(exceptionHandler);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		await sut.EditorLoaded();

		sut.Document.Text += "😀";

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
			.UpdateFilePropertiesAsync(default, default!, default);

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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler.Watch(Arg.Do<Task>(watched.Add));

			builder.RegisterInstance(dbAccess);

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
			.UpdateFilePropertiesAsync(default, default!, default);

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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler.Watch(Arg.Do<Task>(watched.Add));

			builder.RegisterInstance(dbAccess);

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
			.UpdateFilePropertiesAsync(default, default!, default);

		responses
			.Should()
			.Equal(true);
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
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			dbAccess
				.UpdateFilePropertiesAsync(default, default!, default)
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

			builder.RegisterInstance(contentCipher);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.KeeperId = Guid.NewGuid();

		await sut.EditorLoaded();

		string text = sut.Document.Text;

		sut.Document.Text = $"{text}😀";

		FlushEditorsMessage refused = new();

		sut.Receive(refused);

		await refused.GetResponsesAsync();

		sut.Document.Text = $"{text}x";

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
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			dbAccess
				.UpdateFilePropertiesAsync(default, default!, default)
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

			builder.RegisterInstance(contentCipher);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.KeeperId = Guid.NewGuid();

		await sut.EditorLoaded();

		sut.Document.Text = text;

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
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			dbAccess
				.UpdateFilePropertiesAsync(default, default!, default)
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

			builder.RegisterInstance(contentCipher);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.KeeperId = Guid.NewGuid();

		await sut.EditorLoaded();

		sut.Document.Text = text;

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
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			dbAccess
				.UpdateFilePropertiesAsync(default, default!, default)
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

			builder.RegisterInstance(contentCipher);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.KeeperId = Guid.NewGuid();

		await sut.EditorLoaded();

		sut.Document.Text += "x";

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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

			exceptionHandler.Watch(Arg.Do<Task>(watched.Add));

			builder.RegisterInstance(dbAccess);

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
			.UpdateFilePropertiesAsync(default, default!, default);

		responses
			.Should()
			.Equal(true);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.SplitShare" />: a new share of a split document is reported.
	/// </summary>
	[AvaloniaTest]
	public void SplitShare_Reports_The_New_Share_While_Split()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.IsSplit = true;

		List<double?> reported = [];

		sut.SetEditorSplitCallback = reported.Add;

		// Act
		sut.SplitShare = 0.25;

		// Assert
		reported
			.Should()
			.Equal(0.25);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.SyntaxLanguage" />: a return to the language of the extension stores no choice.
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.FileName = fileName;

		sut.SetEditorStateCallback = x => reported = x;

		await sut.EditorLoaded();

		sut.SyntaxLanguage = "bat";

		// Act
		sut.SyntaxLanguage = defaultLanguage;

		// Assert
		string? stored = new SystemTextJsonSerializer().Deserialize<FileEditorState>(reported!).SyntaxLanguage;

		stored
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.SyntaxLanguage" />: a language other than the one of the extension is stored
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.FileName = fileName;

		sut.SetEditorStateCallback = x => reported = x;

		await sut.EditorLoaded();

		// Act
		sut.SyntaxLanguage = chosen;

		// Assert
		string? stored = new SystemTextJsonSerializer().Deserialize<FileEditorState>(reported!).SyntaxLanguage;

		stored
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.ViewState" />: the folded and unfolded blocks of a protected file stay out of
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			// A protected file hands its contents to the cipher, which gives them back as they are.
			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(x => x.ArgAt<byte[]>(2));

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(contentCipher);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.KeeperId = Guid.NewGuid();

		sut.SetEditorStateCallback = x => reported = x;

		await sut.EditorLoaded();

		// Act
		sut.ViewState = new DocumentViewState
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
	/// <see cref="EmbeddedFileEditorViewModel.ViewState" />: a view state reported after the editor has been closed
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
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		await sut.EditorLoaded();

		sut.Dispose();

		// Act
		sut.ViewState = new DocumentViewState
		{
			CaretPosition = new(line: 3, column: 2),
			ScrollOffset = new(15.0, 480.0),
			SelectionLength = 4,
			SelectionStart = 20
		};

		// Assert
		await dbAccess
			.DidNotReceiveWithAnyArgs()
			.UpdateFilePropertiesAsync(default, default!, default);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.ViewState" />: the bookmarks of a new view state go into the editor state.
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.SetEditorStateCallback = x => reported = x;

		await sut.EditorLoaded();

		// Act
		sut.ViewState = new DocumentViewState
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
	/// <see cref="EmbeddedFileEditorViewModel.ViewState" />: a new view state is saved together with the font size
	/// and the word wrap.
	/// </summary>
	[AvaloniaTest]
	public async Task ViewState_Saves_The_Editor_State()
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
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.SetEditorStateCallback = x => reported = x;

		await sut.EditorLoaded();

		string expected = new SystemTextJsonSerializer().Serialize(
			new FileEditorState
			{
				CaretPosition = new(line: 3, column: 2),
				FontSize = sut.FontSize,
				ScrollOffset = new(15, 480),
				SelectionLength = 4,
				SelectionStart = 20,
				WordWrap = sut.WordWrap
			},
			JsonDefaults.Options);

		// Act
		sut.ViewState = new DocumentViewState
		{
			CaretPosition = new(line: 3, column: 2),
			ScrollOffset = new(15.0, 480.0),
			SelectionLength = 4,
			SelectionStart = 20
		};

		// Assert
		reported
			.Should()
			.Be(expected);

		await dbAccess
			.ReceivedWithAnyArgs(1)
			.UpdateFilePropertiesAsync(default, default!, default);
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.ViewState" />: the folded and unfolded blocks of a new view state go into the
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel sut = mock.Create<EmbeddedFileEditorViewModel>();

		sut.SetEditorStateCallback = x => reported = x;

		await sut.EditorLoaded();

		// Act
		sut.ViewState = new DocumentViewState
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
	#endregion

	#region Helpers
	/// <summary>
	/// File contents that are not text.
	/// </summary>
	private static byte[][] NonTextContents() =>
	[
		// UTF-16 without a byte order mark: valid UTF-8, but with zero bytes
		[0x48, 0x00, 0x69, 0x00],
		// Start of a PNG image: its signature and the length of its first chunk
		[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D],
		// Bytes that are not UTF-8, with control characters that no text holds
		[0xC9, 0x01, 0x02, 0x10]
	];

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
