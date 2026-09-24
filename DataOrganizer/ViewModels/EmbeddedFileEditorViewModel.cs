using Avalonia;
using Avalonia.Controls;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto;
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
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text.Unicode;
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
	/// The file contents as an editable document.
	/// </summary>
	[ObservableProperty]
	public partial TextDocument Document { get; private set; } = new();

	/// <inheritdoc cref="FileEditorState.FontSize" />
	[ObservableProperty]
	public partial double FontSize { get; set; } = 14.0;

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
	/// Caret, selection and scroll position of <see cref="Document" />.
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

			// Contents that are not text would come back from the editor re-encoded and overwrite the file.
			if (!IsText(output))
			{
				IsContentUnavailable = true;

				output.ZeroMemory();

				_notification.ShowWarningSnackbar(Strings.NonTextFileContents);

				_logger.LogWarning($@"{Strings.NonTextFileContents} of file ""{FileId}""");

				return;
			}

			// A new document starts with an empty undo stack, so the loaded text cannot be undone.
			TextDocument document = new(TextDefaults
				.Encoding
				.GetString(output));

			Document = document;

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

		return await isDrained
			.WaitAsync(millisecondsDelay: 100, maxRepeats: 50, token)
			.ConfigureAwait(true) && !Volatile.Read(ref _lastSaveFailed);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when <paramref name="contents" /> is UTF-8 text, which the editor saves back unchanged.
	/// </summary>
	private static bool IsText(ReadOnlySpan<byte> contents)
	{
		// A zero byte is valid UTF-8, yet in a file it marks binary data or UTF-16 text.
		return Utf8.IsValid(contents) && !contents.Contains((byte)0);
	}

	/// <summary>
	/// Creates <see cref="FileEditorState" /> from the view model.
	/// </summary>
	private FileEditorState CreateEditorState()
	{
		DocumentViewState view = ViewState.GetValueOrDefault();

		return new()
		{
			CaretPosition = view.CaretPosition,
			FontSize = FontSize,
			WordWrap = WordWrap,
			ScrollOffset = new((int)view.ScrollOffset.X, (int)view.ScrollOffset.Y),
			SelectionLength = view.SelectionLength,
			SelectionStart = view.SelectionStart,
			ShowEndOfLine = ShowEndOfLine,
			ShowSpaces = ShowSpaces,
			ShowTabs = ShowTabs
		};
	}

	/// <summary>
	/// Queues the current text of the document for saving.
	/// </summary>
	private void EnqueueSave(TextDocument document)
	{
		byte[] contents = TextDefaults
			.Encoding
			.GetBytes(document.Text);

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

			WordWrap = state.WordWrap;

			ViewState = new DocumentViewState
			{
				CaretPosition = state.CaretPosition,
				ScrollOffset = new(state.ScrollOffset.X, state.ScrollOffset.Y),
				SelectionLength = state.SelectionLength,
				SelectionStart = state.SelectionStart
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
