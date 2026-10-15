using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Diagnostics;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Messages.Editor;
using DataOrganizer.Windows;
using Entities.Models;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
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
	public Guid FileId { get; set; }

	/// <summary>
	/// Editor state to start from.
	/// </summary>
	public string? InitialEditorState { get; set; }

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
	public Guid? KeeperId { get; set; }

	/// <summary>
	/// Callback that reports the editor state.
	/// </summary>
	public Action<string>? SetEditorStateCallback { get; set; }

	/// <summary>
	/// Callback that reports the date of change written with an edit of the contents.
	/// </summary>
	public Action<DateTime>? SetUpdatedAtCallback { get; set; }
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Displays object in the list.
	/// </summary>
	[RelayCommand]
	private void ShowInList(Window? window)
	{
		if (window is null)
		{
			return;
		}

		_messenger.Send(new ShowInEditorMessage(FileId, window));
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IEditorStateStore" />
	protected readonly IEditorStateStore _editorStateStore;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	protected readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IFileContentsReader" />
	protected readonly IFileContentsReader _fileContentsReader;

	/// <inheritdoc cref="IJsonSerializer" />
	protected readonly IJsonSerializer _jsonSerializer;

	/// <inheritdoc cref="ILogger" />
	protected readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	protected readonly INotificationService _notification;

	/// <summary>
	/// The editor state last persisted to the database.
	/// Intended to skip persistence when the state matches what is already stored.
	/// </summary>
	protected string? _lastSavedEditorState;

	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IFileContentsWriter" />
	private readonly IFileContentsWriter _fileContentsWriter;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;
	#endregion

	#region Constructors
	protected EmbeddedEditorViewModelBase(
		Application app,
		IEditorStateStore editorStateStore,
		IFileContentsReader fileContentsReader,
		IFileContentsWriter fileContentsWriter,
		IJsonSerializer jsonSerializer,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		ITaskExceptionHandler exceptionHandler)
	{
		_app = app;

		_editorStateStore = editorStateStore;

		_exceptionHandler = exceptionHandler;

		_fileContentsReader = fileContentsReader;

		_fileContentsWriter = fileContentsWriter;

		_jsonSerializer = jsonSerializer;

		_logger = logger;

		_messenger = messenger;

		_notification = notification;

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
	/// Takes the read-only mode over from the editor window.
	/// </summary>
	public void Initialize()
	{
		if (_app.FindWindow<EditorWindow>() is not EditorWindow window)
		{
			return;
		}

		IsReadOnly = window
			.ViewModel
			.IsReadOnly;
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
	/// <c>True</c> when <paramref name="current"/> is equal to <see cref="_lastSavedEditorState" />.
	/// </summary>
	protected bool IsLastEditorStateEqualTo(string current)
	{
		return string.Equals(
			_lastSavedEditorState,
			current,
			StringComparison.Ordinal);
	}

	/// <summary>
	/// Queues for saving the changes that still wait for a pause in typing.
	/// </summary>
	protected virtual void QueuePendingChanges()
	{
	}

	/// <summary>
	/// Saves the contents of the file, encrypted when it is protected, and reports the date of change of an edit.
	/// </summary>
	protected async Task<FileContentsWriteStatus> SaveContentsAsync(
		byte[] contents,
		FileContentsChange change,
		CancellationToken token = default)
	{
		FileContentsWriting writing = await _fileContentsWriter
			.WriteAsync(FileId, KeeperId, contents, change, token)
			.ConfigureAwait(false);

		if (writing.UpdatedAt is { } updatedAt)
		{
			SetUpdatedAtCallback?.Invoke(updatedAt);
		}

		return writing.Status;
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
}
