using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Dto;
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
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(EmbeddedFileEditorViewModel)}"" type")]
internal class EmbeddedFileEditorViewModelTests
{
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
				// "Привет" in Windows-1251
				Contents = [0xCF, 0xF0, 0xE8, 0xE2, 0xE5, 0xF2],
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
	#endregion

	#region Helpers
	/// <summary>
	/// File contents that are not text.
	/// </summary>
	private static byte[][] NonTextContents() =>
	[
		// UTF-16 with a byte order mark
		[0xFF, 0xFE, 0x48, 0x00, 0x69, 0x00],
		// UTF-16 without a byte order mark: valid UTF-8, but with zero bytes
		[0x48, 0x00, 0x69, 0x00],
		// Windows-1251
		[0xCF, 0xF0, 0xE8, 0xE2, 0xE5, 0xF2],
		// Signature of a PNG image
		[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]
	];
	#endregion
}
