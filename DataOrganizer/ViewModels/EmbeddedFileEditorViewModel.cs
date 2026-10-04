using Avalonia;
using Avalonia.Controls;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Notifications;
using Repository.Dto;
using Repository.Interfaces.Database;
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
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>EmbeddedFileEditorView</c>.
/// </summary>
public sealed partial class EmbeddedFileEditorViewModel : EmbeddedEditorViewModelBase
{
	#region Properties
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
	/// Split of the document to start from: the share of the height of the upper half, or <c>null</c> when it is not split.
	/// </summary>
	public double? InitialEditorSplit { get; set; }

	/// <summary>
	/// <c>True</c> when the document is shown in two halves, one above the other.
	/// </summary>
	[ObservableProperty]
	public partial bool IsSplit { get; set; }

	/// <summary>
	/// Callback that reports the split of the document: the share of the height of the upper half, or <c>null</c> when it is not split.
	/// </summary>
	public Action<double?>? SetEditorSplitCallback { get; set; }

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
		if (InitialEditorSplit is { } split)
		{
			SplitShare = split;

			IsSplit = true;
		}

		try
		{
			ValidatedContents result;

			try
			{
				result = await _dbAccess
					.GetFileContentsAsync(FileId)
					.ConfigureAwait(true);
			}
			catch (Exception ex)
			{
				// Nothing was read, so the editor stays closed rather than saving over what it does not hold.
				IsContentUnavailable = true;

				_dbFailureReporter.Report(ex, Strings.FailedToLoadFileContents);

				return;
			}

			if (!result.IsValid)
			{
				IsContentUnavailable = true;

				_notification.ShowErrorSnackbar(Strings.MissingFileContents);

				_logger.LogError(
					$@"{Strings.MissingFileContents} of file ""{FileId}""",
					breakInDebugger: false);

				return;
			}

			if (TryDecrypt(result.Contents) is not { } output)
			{
				IsContentUnavailable = true;

				_notification.ShowErrorSnackbar(Strings.FailedToProcessContents);

				_logger.LogError(
					$@"{Strings.FailedToProcessContents} of file ""{FileId}""",
					breakInDebugger: false);

				return;
			}

			// Contents that are not text, or that their encoding would not write back the same, would overwrite the file.
			if (FileTextCodec.Detect(output) is not { } encoding || FileTextCodec.TryDecode(output, encoding) is not { } text)
			{
				IsContentUnavailable = true;

				output.ZeroMemory();

				_notification.ShowWarningSnackbar(Strings.NonTextFileContents);

				_logger.LogWarning($@"{Strings.NonTextFileContents} of file ""{FileId}""");

				return;
			}

			// The mark belongs to the file rather than to its text: it stays out of the document and returns on save.
			_encoding = encoding;

			// A new document starts with an empty undo stack, so the loaded text cannot be undone.
			TextDocument document = new(text);

			Document = document;

			DefaultSyntaxLanguage = SyntaxRegistry
				.Instance
				.FindLanguage(FileName);

			// The language comes after the text, so the highlighting starts on the document it colors.
			SyntaxLanguage = DefaultSyntaxLanguage;

			EncodingName = encoding.Name;

			_lastSavedContentHash = SHA256.HashData(output);

			try
			{
				await InitializeEditorStateAsync().ConfigureAwait(true);

				TimeSpan delay = TimeSpan.FromSeconds(0.5);

				Observable.FromEventPattern<EventHandler, EventArgs>(
					x => document.TextChanged += x,
					x => document.TextChanged -= x)
					.SetDelay(delay)
					.Subscribe(Document_TextChanged)
					.DisposeWith(_disposables);

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
	#endregion

	#region Data
	/// <inheritdoc cref="IDbFailureReporter" />
	private readonly IDbFailureReporter _dbFailureReporter;

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

	/// <summary>
	/// Encoding of the file, which its text is saved in; <c>null</c> until the contents are loaded.
	/// </summary>
	private FileEncoding? _encoding;

	/// <summary>
	/// <c>True</c> while the text holds a character that the encoding of the file does not have, so it is not saved.
	/// </summary>
	private bool _isTextOutsideEncoding;

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
		IContentCipher contentCipher,
		IDbAccess dbAccess,
		IDbFailureReporter dbFailureReporter,
		IJsonSerializer jsonSerializer,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		ITaskExceptionHandler exceptionHandler) : base(
			app,
			contentCipher,
			dbAccess,
			jsonSerializer,
			logger,
			messenger,
			notification,
			exceptionHandler)
	{
		_dbFailureReporter = dbFailureReporter;
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
	/// Called when <see cref="FontSize" /> changes.
	/// </summary>
	partial void OnFontSizeChanged(double value) => TrySavePersistentEditorState();

	/// <summary>
	/// Called when <see cref="IsSplit" /> changes.
	/// </summary>
	partial void OnIsSplitChanged(bool value) => ReportEditorSplit();

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
	partial void OnSplitShareChanged(double value) => ReportEditorSplit();

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
		if (!IsEditingEnabled)
		{
			return true;
		}

		// The text change handler is debounced, so the newest text may not be queued yet.
		EnqueueSave(Document);

		Func<bool> isDrained = () => Volatile.Read(ref _pendingSaves) == 0;

		// A text that the encoding of the file cannot hold stays unsaved as well.
		return await isDrained
			.WaitAsync(millisecondsDelay: 100, maxRepeats: 50, token)
			.ConfigureAwait(true) && !Volatile.Read(ref _lastSaveFailed) && !_isTextOutsideEncoding;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates <see cref="FileEditorState" /> from the view model.
	/// </summary>
	private FileEditorState CreateEditorState()
	{
		DocumentViewState view = ViewState.GetValueOrDefault();

		return new()
		{
			Bookmarks = view.Bookmarks,
			CaretPosition = view.CaretPosition,
			// The blocks of a protected text would give away its outline, which its ciphertext does not.
			FoldedBlocks = IsEncrypted ? null : view.FoldedBlocks,
			FontSize = FontSize,
			WordWrap = WordWrap,
			ScrollOffset = new((int)view.ScrollOffset.X, (int)view.ScrollOffset.Y),
			SelectionLength = view.SelectionLength,
			SelectionStart = view.SelectionStart,
			ShowEndOfLine = ShowEndOfLine,
			ShowSpaces = ShowSpaces,
			ShowTabs = ShowTabs,
			SyntaxLanguage = GetStoredSyntaxLanguage(),
			UnfoldedBlocks = IsEncrypted ? null : view.UnfoldedBlocks
		};
	}

	/// <summary>
	/// Queues the current text of the document for saving in the encoding of the file; a text with a character that the
	/// encoding does not have stays unsaved.
	/// </summary>
	private void EnqueueSave(TextDocument document)
	{
		if (_encoding is not { } encoding)
		{
			return;
		}

		// The byte order mark taken off the text on load goes back in front of it.
		if (FileTextCodec.TryEncode(document.Text, encoding, out string? missingCharacter) is not { } contents)
		{
			// The warning comes once, not after every pause in typing, until the text fits again.
			if (!_isTextOutsideEncoding)
			{
				_isTextOutsideEncoding = true;

				_notification.ShowWarningSnackbar(string.Format(
					CultureInfo.CurrentCulture,
					Strings.CharacterNotInEncodingFormat,
					missingCharacter,
					encoding.Name));

				_logger.LogWarning($@"The text of file ""{FileId}"" does not fit {encoding.Name} and is not saved");
			}

			return;
		}

		_isTextOutsideEncoding = false;

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
	/// Returns the language to store in the editor state; <c>null</c> while the text keeps the one of the file extension.
	/// </summary>
	private string? GetStoredSyntaxLanguage()
	{
		if (SyntaxLanguage == DefaultSyntaxLanguage)
		{
			return null;
		}

		return SyntaxLanguage ?? FileEditorState.PlainTextLanguage;
	}

	/// <summary>
	/// Returns the language of the text for the one stored in the editor state.
	/// </summary>
	private string? GetSyntaxLanguage(string? stored)
	{
		if (stored is null)
		{
			return DefaultSyntaxLanguage;
		}

		// Plain text is stored as a language without a grammar, and so is a language whose grammar is gone.
		return SyntaxRegistry.Instance.FindScope(stored) is null ? null : stored;
	}

	/// <summary>
	/// Restores the editor state from the database.
	/// </summary>
	private async Task InitializeEditorStateAsync(CancellationToken token = default)
	{
		// The restored values reach the view through bindings, so they are set on the UI thread.
		string? value = InitialEditorState ?? await _dbAccess
			.GetFileEditorStateAsync(FileId, token)
			.ConfigureAwait(true);

		if (value is null)
		{
			return;
		}

		try
		{
			FileEditorState state = _jsonSerializer.Deserialize<FileEditorState>(value);

			FontSize = state.FontSize;

			ShowEndOfLine = state.ShowEndOfLine;

			ShowSpaces = state.ShowSpaces;

			ShowTabs = state.ShowTabs;

			SyntaxLanguage = GetSyntaxLanguage(state.SyntaxLanguage);

			WordWrap = state.WordWrap;

			ViewState = new DocumentViewState
			{
				Bookmarks = state.Bookmarks,
				CaretPosition = state.CaretPosition,
				FoldedBlocks = state.FoldedBlocks,
				ScrollOffset = new(state.ScrollOffset.X, state.ScrollOffset.Y),
				SelectionLength = state.SelectionLength,
				SelectionStart = state.SelectionStart,
				UnfoldedBlocks = state.UnfoldedBlocks
			};

			_logger.LogDebug(
				$@"Editor state of ""{FileId}"" is initialized:{state.GetPropertyValues(true)}");
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
		}
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

				if (TryEncrypt(latest) is not { } output)
				{
					_notification.ShowErrorSnackbar(Strings.FailedToProcessContents);

					latest.ZeroMemory();

					Volatile.Write(ref _lastSaveFailed, true);

					continue;
				}

				try
				{
					bool isSaved = await SaveContentsAsync(output).ConfigureAwait(false);

					if (isSaved)
					{
						_lastSavedContentHash = hash;
					}

					Volatile.Write(ref _lastSaveFailed, !isSaved);
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

					output.ZeroMemory();
				}
			}
			finally
			{
				Interlocked.Add(ref _pendingSaves, -takenCount);
			}
		}
	}

	/// <summary>
	/// Reports the split of the document through <see cref="SetEditorSplitCallback" />.
	/// </summary>
	private void ReportEditorSplit() => SetEditorSplitCallback?.Invoke(IsSplit ? SplitShare : null);

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
