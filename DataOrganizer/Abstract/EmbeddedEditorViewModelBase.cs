using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
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

namespace DataOrganizer.Abstract;

public abstract partial class EmbeddedEditorViewModelBase :
	ObservableDisposableBase,
	IRecipient<EditorReadOnlyModeChangedMessage>
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
	/// Returns <c>True</c> if the initialization process revealed that the file contents were corrupted.
	/// </summary>
	public bool IsContentCorrupted { get; protected set; }

	/// <summary>
	/// Returns <c>True</c> if editor is initialized once.
	/// </summary>
	public bool IsInitialized { get; protected set; }

	/// <summary>
	/// Read-only mode.
	/// </summary>
	[ObservableProperty]
	public partial bool IsReadOnly { get; set; }

	/// <summary>
	/// Encrypted within the session DEK.
	/// </summary>
	public byte[]? SessionEncryptedDek { get; set; }

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

		_messenger.Send(new ShowInEditorMessage(new(FileId, window)));
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IDbAccess" />
	protected readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	protected readonly ITaskExceptionHandler _handler;

	/// <inheritdoc cref="IJsonSerializerWrapper" />
	protected readonly IJsonSerializerWrapper _jsonSerializer;

	/// <inheritdoc cref="ILogger" />
	protected readonly ILogger _logger;

	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IEntityEncryption" />
	private readonly IEntityEncryption _entityEncryption;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;
	#endregion

	#region Constructors
	protected EmbeddedEditorViewModelBase(
		Application app,
		IDbAccess dbAccess,
		IEntityEncryption entityEncryption,
		IJsonSerializerWrapper jsonSerializer,
		ILogger logger,
		IMessenger messenger,
		ITaskExceptionHandler handler)
	{
		_app = app;

		_dbAccess = dbAccess;

		_entityEncryption = entityEncryption;

		_handler = handler;

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
		IsReadOnly = message.Value;
	}

	/// <inheritdoc />
	protected override void AfterDispose()
	{
		base.AfterDispose();

		_messenger.UnregisterAll(this);

		SessionEncryptedDek?.ZeroMemory();

		SessionEncryptedDek = null;
	}

	/// <summary>
	/// Saves <see cref="FileModel.Contents" /> to the database.
	/// </summary>
	protected Task SaveContentsAsync(byte[] contents, CancellationToken token = default)
	{
		_logger.LogDebug($@"Saving contents of ""{FileId}"" in the database");

		return _dbAccess.UpdateFilePropertiesAsync(FileId,
		[
			x => x.SetProperty(x => x.Contents, contents)
		], token);
	}

	/// <summary>
	/// Saves <see cref="FileModel.Properties" /> to the database.
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
	/// Tries to decrypt the content, if it has been decrypted.
	/// </summary>
	protected byte[]? TryToDecrypt(byte[] input)
	{
		if (SessionEncryptedDek is null || input.IsEmpty())
		{
			return input;
		}

		return _entityEncryption.DecryptSessionContents(
			input,
			SessionEncryptedDek);
	}

	/// <summary>
	/// Tries to encrypt the content, if it has been decrypted.
	/// </summary>
	protected byte[]? TryToEncrypt(byte[] input)
	{
		if (SessionEncryptedDek is null || input.IsEmpty())
		{
			return input;
		}

		return _entityEncryption.EncryptSessionContents(
			input,
			SessionEncryptedDek);
	}
	#endregion
}
