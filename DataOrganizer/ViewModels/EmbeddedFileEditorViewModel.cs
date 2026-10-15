using Avalonia;
using Avalonia.Controls;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Text;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>EmbeddedFileEditorView</c>.
/// </summary>
public sealed partial class EmbeddedFileEditorViewModel : EmbeddedEditorViewModelBase, IEditorStateValues
{
	#region Properties
	/// <summary>
	/// Web name of the encoding that the contents of the file are found to be in; <c>null</c> when they are not found to be
	/// text.
	/// </summary>
	[ObservableProperty]
	public partial string? DefaultEncoding { get; private set; }

	/// <summary>
	/// Language that the extension of the file gives its text; <c>null</c> for plain text.
	/// </summary>
	[ObservableProperty]
	public partial string? DefaultSyntaxLanguage { get; private set; }

	/// <summary>
	/// The file contents as an editable document.
	/// </summary>
	[ObservableProperty]
	public partial TextDocument Document { get; private set; } = new();

	/// <summary>
	/// Web name of the encoding the text is read in and saved in; another one reads the text again from the same bytes.
	/// </summary>
	[ObservableProperty]
	public partial string? Encoding { get; set; }

	/// <summary>
	/// Name of the encoding the file is stored in.
	/// </summary>
	[ObservableProperty]
	public partial string? EncodingName { get; private set; }

	/// <summary>
	/// Name of the file, whose extension gives the language of its text.
	/// </summary>
	public string? FileName { get; set; }

	/// <inheritdoc cref="FileEditorState.FontSize" />
	[ObservableProperty]
	public partial double FontSize { get; set; } = 14.0;

	/// <summary>
	/// <c>True</c> when the document is shown in two halves, one above the other.
	/// </summary>
	[ObservableProperty]
	public partial bool IsSplit { get; set; }

	/// <inheritdoc cref="FileEditorState.ShowEndOfLine" />
	[ObservableProperty]
	public partial bool ShowEndOfLine { get; set; }

	/// <inheritdoc cref="FileEditorState.ShowSpaces" />
	[ObservableProperty]
	public partial bool ShowSpaces { get; set; }

	/// <inheritdoc cref="FileEditorState.ShowTabs" />
	[ObservableProperty]
	public partial bool ShowTabs { get; set; }

	/// <summary>
	/// Share of the height that the upper half takes while the document is split.
	/// </summary>
	[ObservableProperty]
	public partial double SplitShare { get; set; } = 0.5;

	/// <summary>
	/// Language of the text for the syntax highlighting; <c>null</c> for plain text.
	/// </summary>
	[ObservableProperty]
	public partial string? SyntaxLanguage { get; set; }

	/// <summary>
	/// Web names of the encodings that cannot read the text as the file stores it; <c>null</c> when they are not known.
	/// </summary>
	[ObservableProperty]
	public partial IReadOnlyCollection<string>? UnreadableEncodings { get; private set; }

	/// <summary>
	/// Caret, selection, scroll position, bookmarks and folded blocks of <see cref="Document" />.
	/// </summary>
	[ObservableProperty]
	public partial DocumentViewState? ViewState { get; set; }

	/// <inheritdoc cref="FileEditorState.WordWrap" />
	[ObservableProperty]
	public partial bool WordWrap { get; set; }
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Handles the <see cref="Control.Loaded" /> event of the editor.
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
			SplitShare = split;

			IsSplit = true;
		}

		try
		{
			FileContentsReading reading;

			try
			{
				reading = await _fileContentsReader
					.ReadUnlockedAsync(FileId, KeeperId)
					.ConfigureAwait(true);
			}
			catch (Exception ex)
			{
				// Nothing was read, so the editor stays closed rather than saving over what it does not hold.
				IsContentUnavailable = true;

				_dbFailureReporter.Report(ex, Strings.FailedToLoadFileContents);

				return;
			}

			if (reading.Status == FileContentsStatus.Damaged)
			{
				IsContentUnavailable = true;

				_notification.ShowErrorSnackbar(Strings.MissingFileContents);

				_logger.LogError(
					$@"{Strings.MissingFileContents} of file ""{FileId}""",
					breakInDebugger: false);

				return;
			}

			if (reading.Status == FileContentsStatus.Closed)
			{
				IsContentUnavailable = true;

				_notification.ShowErrorSnackbar(Strings.FailedToProcessContents);

				_logger.LogError(
					$@"{Strings.FailedToProcessContents} of file ""{FileId}""",
					breakInDebugger: false);

				return;
			}

			byte[] output = reading.Contents;

			try
			{
				// The state comes before the text, as it holds the encoding chosen for the text.
				FileEditorState? state = await ReadEditorStateAsync().ConfigureAwait(true);

				// Contents that are not text, or that their encoding would not write back the same, would overwrite the file.
				if (_codec.Read(output, state?.Encoding) is not { } text)
				{
					IsContentUnavailable = true;

					_notification.ShowWarningSnackbar(Strings.NonTextFileContents);

					_logger.LogWarning($@"{Strings.NonTextFileContents} of file ""{FileId}""");

					return;
				}

				// A new document starts with an empty undo stack, so the loaded text cannot be undone.
				SetDocument(new(text));

				DefaultSyntaxLanguage = _syntaxRegistry.FindLanguage(FileName);

				// The language comes after the text, so the highlighting starts on the document it colors.
				SyntaxLanguage = DefaultSyntaxLanguage;

				DefaultEncoding = _codec.DefaultEncoding;

				Encoding = _codec.Encoding;

				EncodingName = _codec.EncodingName;

				_lastSavedContentHash = SHA256.HashData(output);

				ApplyEditorState(state);

				_exceptionHandler.Watch(Task.Run(() => ProcessSaveChannelAsync()));

				_logger.LogInformation($@"Content is initialized in ""{GetType().Name}""");
			}
			finally
			{
				output.ZeroMemory();
			}
		}
		catch (Exception ex)
		{
			IsContentUnavailable = true;

			_logger.LogException(ex, breakInDebugger: false);

			_notification.ShowErrorSnackbar(Strings.FailedToProcessContents);
		}
		finally
		{
			IsInitialized = true;
		}
	}

	/// <summary>
	/// Finds <see cref="UnreadableEncodings" /> for the text as it is now.
	/// </summary>
	[RelayCommand]
	internal void FindUnreadableEncodings() => UnreadableEncodings = _codec.FindUnreadableEncodings(Document.Text);
	#endregion

	#region Data
	/// <summary>
	/// Encoding of the file, which its text is read in and saved in.
	/// </summary>
	private readonly DocumentCodec _codec = new();

	/// <inheritdoc cref="IDbFailureReporter" />
	private readonly IDbFailureReporter _dbFailureReporter;

	/// <summary>
	/// Subscription to the pauses in the typing of <see cref="Document" />, which a new document replaces.
	/// </summary>
	private readonly SerialDisposable _documentChanges = new();

	/// <inheritdoc cref="IEditorSplitStore" />
	private readonly IEditorSplitStore _editorSplits;

	/// <inheritdoc cref="Lock" />
	private readonly Lock _mutex = new();

	/// <summary>
	/// Channel for save operations.
	/// </summary>
	private readonly Channel<byte[]> _saveChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
	{
		SingleReader = true,
		SingleWriter = true
	});

	/// <inheritdoc cref="ISyntaxRegistry" />
	private readonly ISyntaxRegistry _syntaxRegistry;

	/// <summary>
	/// SHA-256 of the last plain text persisted to the database.
	/// Intended to skip persistence when the text matches what is already stored.
	/// </summary>
	private byte[]? _lastSavedContentHash;

	/// <summary>
	/// <c>True</c> when the last processed save did not reach the database.
	/// </summary>
	private bool _lastSaveFailed;

	/// <summary>
	/// Number of queued contents the save channel consumer has not processed yet.
	/// </summary>
	private int _pendingSaves;
	#endregion

	#region Constructors
	public EmbeddedFileEditorViewModel(
		Application app,
		IDbFailureReporter dbFailureReporter,
		IEditorSplitStore editorSplits,
		IEditorStateStore editorStateStore,
		IFileContentsReader fileContentsReader,
		IFileContentsWriter fileContentsWriter,
		IJsonSerializer jsonSerializer,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		ISyntaxRegistry syntaxRegistry,
		ITaskExceptionHandler exceptionHandler) : base(
			app,
			editorStateStore,
			fileContentsReader,
			fileContentsWriter,
			jsonSerializer,
			logger,
			messenger,
			notification,
			exceptionHandler)
	{
		_dbFailureReporter = dbFailureReporter;

		_editorSplits = editorSplits;

		_syntaxRegistry = syntaxRegistry;

		_documentChanges.DisposeWith(_disposables);
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

	#region Partial
	/// <summary>
	/// Called when <see cref="Encoding" /> changes.
	/// </summary>
	partial void OnEncodingChanged(string? value)
	{
		// The encoding of the file comes back here when it loads and when a choice is refused.
		if (_codec.Encoding is not { } current
			|| value is null
			|| value == current)
		{
			return;
		}

		_exceptionHandler.Watch(ChangeEncodingAsync(value));
	}

	/// <summary>
	/// Called when <see cref="FontSize" /> changes.
	/// </summary>
	partial void OnFontSizeChanged(double value) => TrySavePersistentEditorState();

	/// <summary>
	/// Called when <see cref="IsSplit" /> changes.
	/// </summary>
	partial void OnIsSplitChanged(bool value) => StoreEditorSplit();

	/// <summary>
	/// Called when <see cref="ShowEndOfLine" /> changes.
	/// </summary>
	partial void OnShowEndOfLineChanged(bool value) => TrySavePersistentEditorState();

	/// <summary>
	/// Called when <see cref="ShowSpaces" /> changes.
	/// </summary>
	partial void OnShowSpacesChanged(bool value) => TrySavePersistentEditorState();

	/// <summary>
	/// Called when <see cref="ShowTabs" /> changes.
	/// </summary>
	partial void OnShowTabsChanged(bool value) => TrySavePersistentEditorState();

	/// <summary>
	/// Called when <see cref="SplitShare" /> changes.
	/// </summary>
	partial void OnSplitShareChanged(double value) => StoreEditorSplit();

	/// <summary>
	/// Called when <see cref="SyntaxLanguage" /> changes.
	/// </summary>
	partial void OnSyntaxLanguageChanged(string? value) => TrySavePersistentEditorState();

	/// <summary>
	/// Called when <see cref="ViewState" /> changes.
	/// </summary>
	partial void OnViewStateChanged(DocumentViewState? value) => TrySavePersistentEditorState();

	/// <summary>
	/// Called when <see cref="WordWrap" /> changes.
	/// </summary>
	partial void OnWordWrapChanged(bool value) => TrySavePersistentEditorState();
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void AfterDispose()
	{
		// No pause in typing comes after the close, so an edit still waiting for one is queued now.
		QueuePendingChanges();

		if (!_saveChannel
			.Reader
			.Completion
			.IsCompleted)
		{
			_saveChannel
				.Writer
				.Complete();
		}

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
			EnqueueSave(Document);
		}

		Func<bool> isDrained = () => Volatile.Read(ref _pendingSaves) == 0;

		// A text that the encoding of the file cannot hold stays unsaved as well.
		return await isDrained
			.WaitAsync(millisecondsDelay: 100, maxRepeats: 50, token)
			.ConfigureAwait(true) && !Volatile.Read(ref _lastSaveFailed) && !_codec.IsTextOutsideEncoding;
	}

	/// <inheritdoc />
	protected override void QueuePendingChanges()
	{
		// While the contents are loading the document is still empty, and queuing its text would overwrite the file.
		if (!IsEditingEnabled)
		{
			return;
		}

		EnqueueSave(Document);
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

		EditorStateMapper.Apply(this, value, _syntaxRegistry);

		_logger.LogDebug(
			$@"Editor state of ""{FileId}"" is initialized:{value.GetPropertyValues(true)}");
	}

	/// <summary>
	/// Reads the text again from the same bytes in another encoding; an encoding that cannot read them leaves the text as
	/// it was.
	/// </summary>
	private async Task ChangeEncodingAsync(string name)
	{
		// The text is read again from its saved bytes, which an edit that is not saved would not match.
		if (!await FlushAsync().ConfigureAwait(true) || _codec.Reread(Document.Text, name) is not { } change)
		{
			Encoding = _codec.Encoding;

			_notification.ShowWarningSnackbar(Strings.EncodingNotChanged);

			return;
		}

		if (change.Text is not { } text)
		{
			Encoding = _codec.Encoding;

			_notification.ShowWarningSnackbar(string.Format(
				CultureInfo.CurrentCulture,
				Strings.ContentsUnreadableInEncodingFormat,
				change.Name));

			_logger.LogWarning($@"The contents of file ""{FileId}"" cannot be read in {name}");

			return;
		}

		// The text read in another encoding is not an edit, so a new document starts with an empty undo stack.
		SetDocument(new(text));

		EncodingName = change.Name;

		TrySavePersistentEditorState();
	}

	/// <summary>
	/// Creates <see cref="FileEditorState" /> from the view model.
	/// </summary>
	private FileEditorState CreateEditorState() => EditorStateMapper.Create(this, _codec.StoredEncoding, IsEncrypted);

	/// <summary>
	/// Queues the current text of the document for saving in the encoding of the file; a text with a character that the
	/// encoding does not have stays unsaved.
	/// </summary>
	private void EnqueueSave(TextDocument document)
	{
		bool wasTextOutsideEncoding = _codec.IsTextOutsideEncoding;

		// The byte order mark taken off the text on load goes back in front of it.
		if (_codec.Encode(document.Text, out string? missingCharacter) is not { } contents)
		{
			// The warning comes once, not after every pause in typing, until the text fits again.
			if (missingCharacter is not null && !wasTextOutsideEncoding)
			{
				_notification.ShowWarningSnackbar(string.Format(
					CultureInfo.CurrentCulture,
					Strings.CharacterNotInEncodingFormat,
					missingCharacter,
					_codec.EncodingName));

				_logger.LogWarning($@"The text of file ""{FileId}"" does not fit {_codec.EncodingName} and is not saved");
			}

			return;
		}

		if (_saveChannel
			.Writer
			.TryWrite(contents))
		{
			Interlocked.Increment(ref _pendingSaves);

			return;
		}

		contents.ZeroMemory();
	}

	/// <summary>
	/// Background consumer that processes the save channel sequentially.
	/// Drains all queued items and saves only the latest.
	/// </summary>
	private async Task ProcessSaveChannelAsync()
	{
		ChannelReader<byte[]> reader = _saveChannel.Reader;

		await foreach (byte[] contents in reader
			.ReadAllAsync()
			.ConfigureAwait(false))
		{
			// Drain the channel — keep only the latest, ZeroMemory the rest.
			byte[] latest = contents;

			// Counted, not decremented yet: the batch stays pending until it has been persisted.
			int takenCount = 1;

			try
			{
				// Insurance in case of:
				// - slow encryption (large file)
				// - slow DB (disk under load)
				// - quick paste (Ctrl+V of large text can cause several TextChanged in a row)
				while (reader.TryRead(out byte[]? newer))
				{
					takenCount++;

					latest.ZeroMemory();

					latest = newer;
				}

				byte[] hash = SHA256.HashData(latest);

				if (_lastSavedContentHash is { } previous && hash.AsSpan().SequenceEqual(previous))
				{
					latest.ZeroMemory();

					Volatile.Write(ref _lastSaveFailed, false);

					continue;
				}

				try
				{
					FileContentsWriteStatus status = await SaveContentsAsync(latest, FileContentsChange.Edit)
						.ConfigureAwait(false);

					if (status == FileContentsWriteStatus.Written)
					{
						_lastSavedContentHash = hash;
					}
					else if (status == FileContentsWriteStatus.Closed)
					{
						_notification.ShowErrorSnackbar(Strings.FailedToProcessContents);
					}

					Volatile.Write(ref _lastSaveFailed, status != FileContentsWriteStatus.Written);
				}
				catch (Exception ex)
				{
					// The loop has to survive a failed save: the editor keeps the text and marks it unsaved.
					_logger.LogException(ex, breakInDebugger: false);

					Volatile.Write(ref _lastSaveFailed, true);
				}
				finally
				{
					latest.ZeroMemory();
				}
			}
			finally
			{
				Interlocked.Add(ref _pendingSaves, -takenCount);
			}
		}
	}

	/// <summary>
	/// Reads the editor state from the database; <c>null</c> when there is none or it cannot be read, in which case a new
	/// one replaces it.
	/// </summary>
	private async Task<FileEditorState?> ReadEditorStateAsync(CancellationToken token = default)
	{
		// The restored values reach the view through bindings, so they are set on the UI thread.
		string? value = InitialEditorState ?? await _editorStateStore
			.ReadAsync(FileId, token)
			.ConfigureAwait(true);

		if (value is null)
		{
			return null;
		}

		try
		{
			return _jsonSerializer.Deserialize<FileEditorState>(value);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			if (!IsReadOnly)
			{
				await SaveEditorStateAsync(
					_jsonSerializer.Serialize(CreateEditorState(), JsonDefaults.Options),
					token);
			}

			return null;
		}
	}

	/// <summary>
	/// Makes a document the one being edited, whose text is saved after each pause in typing.
	/// </summary>
	private void SetDocument(TextDocument document)
	{
		Document = document;

		_documentChanges.Disposable = Observable.FromEventPattern<EventHandler, EventArgs>(
			x => document.TextChanged += x,
			x => document.TextChanged -= x)
			.SetDelay(TimeSpan.FromSeconds(0.5))
			.Subscribe(Document_TextChanged);
	}

	/// <summary>
	/// Keeps the split of the document for the file while the application runs.
	/// </summary>
	private void StoreEditorSplit() => _editorSplits.Set(FileId, IsSplit ? SplitShare : null);

	/// <summary>
	/// Tries to save the editor state.
	/// </summary>
	private Task TrySaveEditorStateAsync(CancellationToken token = default)
	{
		string json = _jsonSerializer.Serialize(CreateEditorState(), JsonDefaults.Options);

		SetEditorStateCallback?.Invoke(json);

		if (IsReadOnly || IsLastEditorStateEqualTo(json))
		{
			return Task.CompletedTask;
		}

		_lastSavedEditorState = json;

		return SaveEditorStateAsync(json, token);
	}

	/// <summary>
	/// Persists the editor state once the editor is ready.
	/// </summary>
	private void TrySavePersistentEditorState()
	{
		lock (_mutex)
		{
			// The view may still report its state for a moment after the file has been closed.
			if (IsContentUnavailable
				|| !IsInitialized
				|| IsDisposed)
			{
				return;
			}

			_exceptionHandler.Watch(TrySaveEditorStateAsync());
		}
	}
	#endregion
}
