using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Messages;
using DataOrganizer.Windows;
using Entities.Models;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
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
	/// File identifier.
	/// </summary>
	public Guid FileId { get; set; }

	/// <summary>
	/// Initial properties.
	/// </summary>
	public string? InitialProperties { get; set; }

	/// <summary>
	/// <c>True</c> when the initialization process revealed that the file contents were corrupted.
	/// </summary>
	public bool IsContentCorrupted { get; protected set; }

	/// <summary>
	/// <c>True</c> when the file contents are encrypted with a session key.
	/// </summary>
	public bool IsEncrypted => KeeperId is not null;

	/// <summary>
	/// <c>True</c> when the editor has been initialized at least once.
	/// </summary>
	public bool IsInitialized { get; protected set; }

	/// <summary>
	/// Read-only mode.
	/// </summary>
	[ObservableProperty]
	public partial bool IsReadOnly { get; set; }

	/// <summary>
	/// Identifier of the password keeper holding the key of the file.
	/// </summary>
	public Guid? KeeperId { get; set; }

	/// <summary>
	/// Callback to set object's properties.
	/// </summary>
	public Action<string>? SetPropertiesCallback { get; set; }

	/// <summary>
	/// Callback to set object's updated date.
	/// </summary>
	public Action<DateTime>? SetUpdatedDateCallback { get; set; }
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
	/// <inheritdoc cref="IDbAccess" />
	protected readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	protected readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IJsonSerializerWrapper" />
	protected readonly IJsonSerializerWrapper _jsonSerializer;

	/// <inheritdoc cref="ILogger" />
	protected readonly ILogger _logger;

	/// <summary>
	/// Last properties persisted to the database.
	/// Intended to skip persistence when properties match what is already stored.
	/// </summary>
	protected string? _lastSavedProperties;

	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IContentCipher" />
	private readonly IContentCipher _contentCipher;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;
	#endregion

	#region Constructors
	protected EmbeddedEditorViewModelBase(
		Application app,
		IContentCipher contentCipher,
		IDbAccess dbAccess,
		IJsonSerializerWrapper jsonSerializer,
		ILogger logger,
		IMessenger messenger,
		ITaskExceptionHandler exceptionHandler)
	{
		_app = app;

		_dbAccess = dbAccess;

		_contentCipher = contentCipher;

		_exceptionHandler = exceptionHandler;

		_jsonSerializer = jsonSerializer;

		_logger = logger;

		_messenger = messenger;

		messenger.RegisterAll(this);
	}
	#endregion

	#region Methods
	/// <summary>
	/// Performs initialization.
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

		if (MessengerHelper.FormatUnsubscriptionLog(this) is { } logLine)
		{
			_logger.LogDebug(logLine);
		}

		_messenger.UnregisterAll(this);

		KeeperId = null;
	}

	/// <summary>
	/// Persists the pending changes of the editor. <c>False</c> when the contents could not be saved,
	/// which keeps the caller from dropping the key.
	/// </summary>
	protected virtual Task<bool> FlushAsync(CancellationToken token = default) => Task.FromResult(true);

	/// <summary>
	/// <c>True</c> when <paramref name="current"/> is equal to <see cref="_lastSavedProperties" />.
	/// </summary>
	protected bool IsLastPropertiesEqualTo(string current)
	{
		return string.Equals(
			_lastSavedProperties,
			current,
			StringComparison.Ordinal);
	}

	/// <summary>
	/// Saves <see cref="FileModel.Contents" /> in the database.
	/// </summary>
	/// <returns><c>true</c> when the row was updated.</returns>
	protected Task<bool> SaveContentsAsync(byte[] contents, CancellationToken token = default)
	{
		_logger.LogDebug($@"Saving contents of ""{FileId}"" in the database");

		return _dbAccess.UpdateFilePropertiesAsync(FileId,
		[
			x => x.SetProperty(x => x.Contents, contents)
		], token);
	}

	/// <summary>
	/// Saves <see cref="FileModel.Properties" /> in the database.
	/// </summary>
	protected Task SavePropertiesAsync(
		[StringSyntax(StringSyntaxAttribute.Json)] string json,
		CancellationToken token = default)
	{
		_logger.LogDebug(
			$@"Saving properties of ""{FileId}"" in the database:{json}");

		return _dbAccess.UpdateFilePropertiesAsync(FileId,
		[
			x => x.SetProperty(x => x.Properties, json)
		], token);
	}

	/// <summary>
	/// Sends <see cref="ShowSnackbarMessage" /> to recepient.
	/// </summary>
	protected void SendMessage(string message, SnackbarMessageLevel level)
	{
		_messenger.Send(new ShowSnackbarMessage(message, level));
	}

	/// <summary>
	/// Decrypts the content when the editor holds a protected file; <c>null</c> reports a refusal.
	/// </summary>
	protected byte[]? TryToDecrypt(byte[] input)
	{
		if (KeeperId is not { } keeperId || input.IsEmpty())
		{
			return input;
		}

		return _contentCipher.TryDecrypt(
			keeperId,
			ContentIdentity.ForContents(FileId),
			input);
	}

	/// <summary>
	/// Encrypts the content when the editor holds a protected file; <c>null</c> reports a refusal.
	/// </summary>
	protected byte[]? TryToEncrypt(byte[] input)
	{
		if (KeeperId is not { } keeperId || input.IsEmpty())
		{
			return input;
		}

		return _contentCipher.TryEncrypt(
			keeperId,
			ContentIdentity.ForContents(FileId),
			input);
	}
	#endregion
}
