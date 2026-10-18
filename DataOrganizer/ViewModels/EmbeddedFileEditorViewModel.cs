using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Documents;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Text;
using DynamicData.Binding;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>EmbeddedFileEditorView</c>.
/// </summary>
public sealed partial class EmbeddedFileEditorViewModel : EmbeddedEditorViewModelBase
{
	#region Properties
	/// <summary>
	/// The text of the file with its encoding and the switches of the editor; the language that the extension of the file
	/// gives is its default one.
	/// </summary>
	public DocumentEditorViewModel Editor { get; } = new();

	/// <summary>
	/// Name of the file, whose extension gives the language of its text.
	/// </summary>
	public string? FileName { get; private set; }
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Reads the contents, the split and the stored editor state once.
	/// </summary>
	[RelayCommand]
	internal async Task EditorLoaded()
	{
		if (IsInitialized)
		{
			return;
		}

		// The file keeps the split for the session only, apart from the editor state in the database.
		if (_editorSplits.Find(FileId) is { } split)
		{
			Editor.SplitShare = split;

			Editor.IsSplit = true;
		}

		await LoadAsync().ConfigureAwait(true);
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IEditorSplitStore" />
	private readonly IEditorSplitStore _editorSplits;

	/// <inheritdoc cref="IDocumentEncodingSwitcher" />
	private readonly IDocumentEncodingSwitcher _encodingSwitcher;

	/// <inheritdoc cref="Lock" />
	private readonly Lock _mutex = new();

	/// <inheritdoc cref="ISyntaxRegistry" />
	private readonly ISyntaxRegistry _syntaxRegistry;

	/// <inheritdoc cref="ContentsWriteQueue" />
	private readonly ContentsWriteQueue _writeQueue;
	#endregion

	#region Constructors
	public EmbeddedFileEditorViewModel(
		IDbFailureReporter dbFailureReporter,
		IDocumentEncodingSwitcher encodingSwitcher,
		IEditorSplitStore editorSplits,
		IEditorStateStore editorStateStore,
		IFileContentsReader fileContentsReader,
		IFileContentsWriter fileContentsWriter,
		IJsonSerializer jsonSerializer,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		ISessionReadOnlyMode sessionReadOnlyMode,
		ISyntaxRegistry syntaxRegistry,
		ITaskExceptionHandler exceptionHandler) : base(
			dbFailureReporter,
			editorStateStore,
			fileContentsReader,
			fileContentsWriter,
			jsonSerializer,
			logger,
			messenger,
			notification,
			sessionReadOnlyMode,
			exceptionHandler)
	{
		_encodingSwitcher = encodingSwitcher;

		_editorSplits = editorSplits;

		_syntaxRegistry = syntaxRegistry;

		_writeQueue = new(WriteContentsAsync);

		// A change of a value that the editor state keeps saves the state.
		Editor
			.WhenAnyPropertyChanged(
				nameof(DocumentEditorViewModel.FontSize),
				nameof(DocumentEditorViewModel.ShowEndOfLine),
				nameof(DocumentEditorViewModel.ShowSpaces),
				nameof(DocumentEditorViewModel.ShowTabs),
				nameof(DocumentEditorViewModel.SyntaxLanguage),
				nameof(DocumentEditorViewModel.ViewState),
				nameof(DocumentEditorViewModel.WordWrap))
			.Subscribe(_ => TrySavePersistentEditorState())
			.DisposeWith(_disposables);

		Editor
			.WhenAnyPropertyChanged(
				nameof(DocumentEditorViewModel.IsSplit),
				nameof(DocumentEditorViewModel.SplitShare))
			.Subscribe(_ => StoreEditorSplit())
			.DisposeWith(_disposables);

		// A choice of another encoding reads the text again.
		Editor
			.WhenValueChanged(x => x.Encoding, notifyOnInitialValue: false)
			.Subscribe(x => _exceptionHandler.Watch(ChangeEncodingAsync(x)))
			.DisposeWith(_disposables);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="TextDocument.TextChanged" /> event handler.
	/// </summary>
	private void Document_TextChanged(EventPattern<EventArgs> e)
	{
		if (IsContentUnavailable
			|| IsReadOnly
			|| e.Sender is not TextDocument document)
		{
			return;
		}

		EnqueueSave(document);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public override void Initialize(EmbeddedEditorSetup setup)
	{
		base.Initialize(setup);

		FileName = setup.FileName;
	}

	/// <inheritdoc />
	protected override void AfterDispose()
	{
		// No pause in typing comes after the close, so an edit still waiting for one is queued now.
		QueuePendingChanges();

		_writeQueue.Complete();

		base.AfterDispose();
	}

	/// <inheritdoc />
	protected override async Task<bool> FlushAsync(CancellationToken token = default)
	{
		// While the contents are loading the document is still empty, and queuing its text would overwrite the file.
		// Nor is anything written over contents that could not be read.
		if (!IsInitialized || IsContentUnavailable)
		{
			return true;
		}

		// The text change handler is debounced, so the newest text may not be queued yet. The read-only mode queued it
		// as it turned on and saves no change made after.
		if (!IsReadOnly)
		{
			EnqueueSave(Editor.Document);
		}

		// A text that the encoding of the file cannot hold stays unsaved as well.
		return await _writeQueue
			.WaitUntilProcessedAsync(token)
			.ConfigureAwait(true) && !_writeQueue.HasFailed && !Editor.Codec.IsTextOutsideEncoding;
	}

	/// <inheritdoc />
	protected override async Task LoadContentsAsync(byte[] contents)
	{
		try
		{
			// The state comes before the text, as it holds the encoding chosen for the text.
			FileEditorState? state = await ReadEditorStateAsync<FileEditorState>().ConfigureAwait(true);

			// Contents that are not text, or that their encoding would not write back the same, would overwrite the file.
			if (Editor.Codec.Read(contents, state?.Encoding) is not { } text)
			{
				IsContentUnavailable = true;

				_notification.ShowWarningSnackbar(Strings.NonTextFileContents);

				_logger.LogWarning($@"{Strings.NonTextFileContents} of file ""{FileId}""");

				return;
			}

			Editor.ReplaceText(text);

			// The pauses in typing are followed from the loaded text on, so the load itself saves nothing.
			Observable.FromEventPattern<EventHandler, EventArgs>(
				x => Editor.Document.TextChanged += x,
				x => Editor.Document.TextChanged -= x)
				.SetDelay(TimeSpan.FromSeconds(0.5))
				.Subscribe(Document_TextChanged)
				.DisposeWith(_disposables);

			Editor.DefaultSyntaxLanguage = _syntaxRegistry.FindLanguage(FileName);

			// The language comes after the text, so the highlighting starts on the document it colors.
			Editor.SyntaxLanguage = Editor.DefaultSyntaxLanguage;

			Editor.RefreshEncoding();

			_writeQueue.SetStored(contents);

			ApplyEditorState(state);

			_exceptionHandler.Watch(Task.Run(() => _writeQueue.RunAsync()));

			_logger.LogInformation($@"Content is initialized in ""{GetType().Name}""");
		}
		finally
		{
			contents.ZeroMemory();
		}
	}

	/// <inheritdoc />
	protected override void LogUnopenedContents(FileContentsStatus status, string message)
	{
		_logger.LogError(message, breakInDebugger: false);
	}

	/// <inheritdoc />
	protected override void QueuePendingChanges()
	{
		// While the contents are loading the document is still empty, and queuing its text would overwrite the file.
		if (!IsEditingEnabled)
		{
			return;
		}

		EnqueueSave(Editor.Document);
	}

	/// <inheritdoc />
	protected override Task ReplaceUnreadableEditorStateAsync(CancellationToken token = default)
	{
		if (IsReadOnly)
		{
			return Task.CompletedTask;
		}

		return SaveEditorStateAsync(_jsonSerializer.Serialize(CreateEditorState(), JsonDefaults.Options), token);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Restores the editor state read from the database.
	/// </summary>
	private void ApplyEditorState(FileEditorState? state)
	{
		if (state is not { } value)
		{
			return;
		}

		EditorStateMapper.Apply(Editor, value, _syntaxRegistry);

		_logger.LogDebug(
			$@"Editor state of ""{FileId}"" is initialized:{value.GetPropertyValues(true)}");
	}

	/// <summary>
	/// Reads the text again from the same bytes in the encoding chosen for it and keeps the choice in the editor state; an
	/// encoding that cannot read them leaves the text as it was.
	/// </summary>
	private async Task ChangeEncodingAsync(string? encoding)
	{
		if (!await _encodingSwitcher
			.SwitchAsync(
				Editor,
				encoding,
				SnackbarHostIdentifiers.Main,
				$@"file ""{FileId}""")
			.ConfigureAwait(true))
		{
			return;
		}

		TrySavePersistentEditorState();
	}

	/// <summary>
	/// Creates <see cref="FileEditorState" /> from the view model.
	/// </summary>
	private FileEditorState CreateEditorState()
	{
		return EditorStateMapper.Create(
			Editor,
			Editor.Codec.StoredEncoding,
			IsEncrypted);
	}

	/// <summary>
	/// Queues the current text of the document for saving in the encoding of the file; a text with a character that the
	/// encoding does not have stays unsaved.
	/// </summary>
	private void EnqueueSave(TextDocument document)
	{
		bool wasTextOutsideEncoding = Editor.Codec.IsTextOutsideEncoding;

		// The byte order mark taken off the text on load goes back in front of it.
		if (Editor.Codec.Encode(document.Text, out string? missingCharacter) is not { } contents)
		{
			// The warning comes once, not after every pause in typing, until the text fits again.
			if (missingCharacter is not null && !wasTextOutsideEncoding)
			{
				_notification.ShowWarningSnackbar(string.Format(
					CultureInfo.CurrentCulture,
					Strings.CharacterNotInEncodingFormat,
					missingCharacter,
					Editor.Codec.EncodingName));

				_logger.LogWarning(
					$@"The text of file ""{FileId}"" does not fit {Editor.Codec.EncodingName} and is not saved");
			}

			return;
		}

		_writeQueue.Enqueue(contents);
	}

	/// <summary>
	/// Keeps the split of the document for the file while the application runs.
	/// </summary>
	private void StoreEditorSplit() => _editorSplits.Set(FileId, Editor.IsSplit ? Editor.SplitShare : null);

	/// <summary>
	/// Persists the editor state once the editor is ready.
	/// </summary>
	private void TrySavePersistentEditorState()
	{
		lock (_mutex)
		{
			// The view may still report its state for a moment after the file has been closed.
			if (!CanSaveEditorState)
			{
				return;
			}

			SaveEditorState(CreateEditorState());
		}
	}

	/// <summary>
	/// Writes the contents of the text as an edit; <c>false</c> when they did not reach the database.
	/// </summary>
	private async Task<bool> WriteContentsAsync(byte[] contents)
	{
		try
		{
			FileContentsWriteStatus status = await SaveContentsAsync(contents, FileContentsChange.Edit)
				.ConfigureAwait(false);

			if (status == FileContentsWriteStatus.Closed)
			{
				_notification.ShowErrorSnackbar(Strings.FailedToProcessContents);
			}

			return status == FileContentsWriteStatus.Written;
		}
		catch (Exception ex)
		{
			// The queue has to survive a failed save: the editor keeps the text and marks it unsaved.
			_logger.LogException(ex, breakInDebugger: false);

			return false;
		}
	}
	#endregion
}
