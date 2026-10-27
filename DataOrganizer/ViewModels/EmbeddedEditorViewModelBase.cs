using AppInstance.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Helpers.Diagnostics;
using Diagnostics.Interfaces;
using EditorDocuments.Dto;
using EditorDocuments.Interfaces;
using EditorMessages.Messages;
using Entities.Enums;
using Entities.Models;
using FileContents.Dto;
using FileContents.Enums;
using FileContents.Interfaces;
using Notifications.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

public abstract partial class EmbeddedEditorViewModelBase :
	ObservableDisposableBase,
	IRecipient<EditorReadOnlyModeChangedMessage>,
	IRecipient<FlushEditorsMessage>
{
	#region Properties
	/// <summary>
	/// The identifier of the file being edited.
	/// </summary>
	public Guid FileId { get; private set; }

	/// <summary>
	/// Name of the file.
	/// </summary>
	public string? FileName { get; private set; }

	/// <summary>
	/// Editor state to start from.
	/// </summary>
	public string? InitialEditorState { get; private set; }

	/// <summary>
	/// <c>True</c> when the contents could not be read, which closes the editor for changes
	/// so that nothing is written over what was not read.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsEditingEnabled))]
	public partial bool IsContentUnavailable { get; protected set; }

	/// <summary>
	/// <c>True</c> when the contents can be edited: the read-only mode is off,
	/// and the contents are loaded and readable.
	/// </summary>
	public bool IsEditingEnabled => !IsReadOnly && IsInitialized && !IsContentUnavailable;

	/// <summary>
	/// <c>True</c> when the file contents are encrypted with a session key.
	/// </summary>
	public bool IsEncrypted => KeeperId is not null;

	/// <summary>
	/// <c>True</c> when the editor has been initialized at least once.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsEditingEnabled))]
	public partial bool IsInitialized { get; protected set; }

	/// <summary>
	/// <c>True</c> when the read-only mode is on.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsEditingEnabled))]
	public partial bool IsReadOnly { get; set; }

	/// <summary>
	/// Identifier of the password keeper holding the key of the file.
	/// </summary>
	public Guid? KeeperId { get; private set; }

	/// <summary>
	/// Kind of the file: a text file or a dataset.
	/// </summary>
	public EntityKind Kind { get; private set; }

	/// <summary>
	/// Callback that reports the editor state.
	/// </summary>
	public Action<string>? SetEditorStateCallback { get; private set; }

	/// <summary>
	/// Callback that reports the date of change written with an edit of the contents.
	/// </summary>
	public Action<DateTime>? SetUpdatedAtCallback { get; private set; }

	/// <summary>
	/// <c>True</c> while a change of the editor state is saved: the contents are loaded and readable, and the editor is
	/// open.
	/// </summary>
	protected bool CanSaveEditorState => IsInitialized && !IsContentUnavailable && !IsDisposed;
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Displays object in the list.
	/// </summary>
	[RelayCommand]
	private void ShowInList() => _messenger.Send(new ShowInEditorMessage(FileId));
	#endregion

	#region Data
	/// <inheritdoc cref="IDbFailureReporter" />
	protected readonly IDbFailureReporter _dbFailureReporter;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	protected readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IJsonSerializer" />
	protected readonly IJsonSerializer _jsonSerializer;

	/// <inheritdoc cref="ILogger" />
	protected readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	protected readonly INotificationService _notification;

	/// <inheritdoc cref="IEditorStateStore" />
	private readonly IEditorStateStore _editorStateStore;

	/// <inheritdoc cref="IFileContentsOpener" />
	private readonly IFileContentsOpener _fileContentsOpener;

	/// <inheritdoc cref="IFileContentsWriter" />
	private readonly IFileContentsWriter _fileContentsWriter;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="ISessionReadOnlyMode" />
	private readonly ISessionReadOnlyMode _sessionReadOnlyMode;

	/// <summary>
	/// The editor state last persisted to the database.
	/// Intended to skip persistence when the state matches what is already stored.
	/// </summary>
	private string? _lastSavedEditorState;
	#endregion

	#region Constructors
	protected EmbeddedEditorViewModelBase(
		IDbFailureReporter dbFailureReporter,
		IEditorStateStore editorStateStore,
		IFileContentsOpener fileContentsOpener,
		IFileContentsWriter fileContentsWriter,
		IJsonSerializer jsonSerializer,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		ISessionReadOnlyMode sessionReadOnlyMode,
		ITaskExceptionHandler exceptionHandler)
	{
		_dbFailureReporter = dbFailureReporter;

		_editorStateStore = editorStateStore;

		_exceptionHandler = exceptionHandler;

		_fileContentsOpener = fileContentsOpener;

		_fileContentsWriter = fileContentsWriter;

		_jsonSerializer = jsonSerializer;

		_logger = logger;

		_messenger = messenger;

		_notification = notification;

		_sessionReadOnlyMode = sessionReadOnlyMode;

		messenger.RegisterAll(this);
	}
	#endregion

	#region Partial
	/// <summary>
	/// Called before <see cref="IsReadOnly" /> changes.
	/// </summary>
	partial void OnIsReadOnlyChanging(bool value)
	{
		// The mode keeps a pause in typing from saving, so an edit still waiting for one is queued now.
		if (!value)
		{
			return;
		}

		QueuePendingChanges();
	}
	#endregion

	#region Methods
	/// <summary>
	/// Takes the file to edit and the read-only mode of the work session.
	/// </summary>
	public virtual void Initialize(EmbeddedEditorSetup setup)
	{
		FileId = setup.FileId;

		FileName = setup.FileName;

		InitialEditorState = setup.InitialEditorState;

		KeeperId = setup.KeeperId;

		Kind = setup.Kind;

		SetEditorStateCallback = setup.SetEditorStateCallback;

		SetUpdatedAtCallback = setup.SetUpdatedAtCallback;

		IsReadOnly = _sessionReadOnlyMode.IsReadOnly;
	}

	/// <inheritdoc />
	public void Receive(EditorReadOnlyModeChangedMessage message)
	{
		IsReadOnly = message.IsReadOnly;
	}

	/// <inheritdoc />
	public void Receive(FlushEditorsMessage message) => message.Reply(FlushAsync(message.CancellationToken));

	/// <inheritdoc />
	protected override void AfterDispose()
	{
		base.AfterDispose();

		if (MessengerSubscriptions.FormatUnsubscriptionLog(this) is { } logLine)
		{
			_logger.LogDebug(logLine);
		}

		_messenger.UnregisterAll(this);
	}

	/// <summary>
	/// Persists the pending changes of the editor. <c>False</c> when the contents could not be saved,
	/// which keeps the caller from dropping the key.
	/// </summary>
	protected virtual Task<bool> FlushAsync(CancellationToken token = default) => Task.FromResult(true);

	/// <summary>
	/// Reads the contents and hands the opened ones to <see cref="LoadContentsAsync" />; contents that cannot be read,
	/// opened or loaded close the editor for changes and give <c>false</c>. The loading ends either way.
	/// </summary>
	protected async Task<bool> LoadAsync()
	{
		try
		{
			byte[]? contents = await _fileContentsOpener
				.OpenAsync(FileId, KeeperId, FileName, Kind)
				.ConfigureAwait(true);

			if (contents is null)
			{
				// Nothing was opened, so the editor stays closed rather than saving over what it does not hold.
				IsContentUnavailable = true;

				return false;
			}

			await LoadContentsAsync(contents).ConfigureAwait(true);

			return true;
		}
		catch (Exception ex)
		{
			IsContentUnavailable = true;

			_logger.LogException(ex, breakInDebugger: false);

			_notification.ShowErrorSnackbar(Strings.FailedToProcessContents);

			return false;
		}
		finally
		{
			IsInitialized = true;
		}
	}

	/// <summary>
	/// Takes the opened contents into the editor, which wipes them.
	/// </summary>
	protected abstract Task LoadContentsAsync(byte[] contents);

	/// <summary>
	/// Queues for saving the changes that still wait for a pause in typing.
	/// </summary>
	protected virtual void QueuePendingChanges()
	{
	}

	/// <summary>
	/// Reads the stored editor state; <c>null</c> when there is none or it cannot be read, and then
	/// <paramref name="replacement" /> takes its place unless the read-only mode is on.
	/// </summary>
	protected async Task<TState?> ReadEditorStateAsync<TState>(TState replacement, CancellationToken token = default)
		where TState : struct
	{
		string? value = InitialEditorState ?? await _editorStateStore
			.ReadAsync(FileId, token)
			.ConfigureAwait(false);

		if (value is null)
		{
			return null;
		}

		try
		{
			return _jsonSerializer.Deserialize<TState>(value);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			if (!IsReadOnly)
			{
				string json = _jsonSerializer.Serialize(replacement, JsonDefaults.Options);

				await SaveEditorStateAsync(json, token).ConfigureAwait(false);
			}

			return null;
		}
	}

	/// <summary>
	/// Saves the contents of the file, encrypted when it is protected, and reports the date of change of an edit;
	/// <c>false</c> when they are not saved, which the user is told of.
	/// </summary>
	protected async Task<bool> SaveContentsAsync(
		byte[] contents,
		FileContentsChange change,
		CancellationToken token = default)
	{
		FileContentsWriting writing;

		try
		{
			writing = await _fileContentsWriter
				.WriteAsync(FileId, KeeperId, contents, change, token)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_dbFailureReporter.Report(ex, Strings.FailedToSaveFileContents);

			return false;
		}

		if (writing.UpdatedAt is { } updatedAt)
		{
			SetUpdatedAtCallback?.Invoke(updatedAt);
		}

		switch (writing.Status)
		{
			case FileContentsWriteStatus.Closed:
				_logger.LogError($@"The key of the session refused to encrypt the contents of file ""{FileId}"".");

				_notification.ShowErrorSnackbar(Strings.FailedToProcessContents);

				return false;

			case FileContentsWriteStatus.Missing:
				_logger.LogWarning($@"File ""{FileId}"" is no longer in the database; its changes are not saved.");

				_notification.ShowErrorSnackbar(Strings.FileIsMissingChangesNotSaved);

				return false;

			default:
				return true;
		}
	}

	/// <summary>
	/// Reports the editor state to the file and saves it, unless the read-only mode is on or it is the state saved last.
	/// </summary>
	protected void SaveEditorState<TState>(TState state) where TState : struct
	{
		string json = _jsonSerializer.Serialize(state, JsonDefaults.Options);

		SetEditorStateCallback?.Invoke(json);

		if (IsReadOnly || IsLastEditorStateEqualTo(json))
		{
			return;
		}

		_lastSavedEditorState = json;

		_exceptionHandler.Watch(SaveEditorStateAsync(json));
	}

	/// <summary>
	/// Saves <see cref="FileEntity.EditorState" /> in the database.
	/// </summary>
	protected Task SaveEditorStateAsync(
		[StringSyntax(StringSyntaxAttribute.Json)] string json,
		CancellationToken token = default)
	{
		return _editorStateStore.WriteAsync(FileId, json, token);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when <paramref name="current"/> is equal to <see cref="_lastSavedEditorState" />.
	/// </summary>
	private bool IsLastEditorStateEqualTo(string current)
	{
		return string.Equals(
			_lastSavedEditorState,
			current,
			StringComparison.Ordinal);
	}
	#endregion
}
