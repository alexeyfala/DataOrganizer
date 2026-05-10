using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using Entities.Models;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Abstract;

public abstract partial class EmbeddedEditorViewModelBase : ObservableDisposableBase
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

	#region Auto-Generated Properties
	/// <summary>
	/// Read-only mode.
	/// </summary>
	[ObservableProperty]
	private bool _isReadOnly;
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Displays object in the list.
	/// </summary>
	[RelayCommand]
	private void ShowInList(Window? window)
	{
		_viewModel.ExecuteInBaseViewModel(x => _handler.Watch(x.ShowInEditorAsync(window, FileId)));
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

	/// <inheritdoc cref="Lock" />
	protected readonly Lock _mutex = new();

	/// <inheritdoc cref="IViewModelExecutionService" />
	protected readonly IViewModelExecutionService _viewModel;

	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IEntityEcryption" />
	private readonly IEntityEcryption _entityEcryption;
	#endregion

	#region Constructors
	protected EmbeddedEditorViewModelBase(
		Application app,
		IDbAccess dbAccess,
		IEntityEcryption entityEcryption,
		IJsonSerializerWrapper jsonSerializer,
		ILogger logger,
		ITaskExceptionHandler handler,
		IViewModelExecutionService viewModel)
	{
		_app = app;

		_dbAccess = dbAccess;

		_entityEcryption = entityEcryption;

		_handler = handler;

		_jsonSerializer = jsonSerializer;

		_logger = logger;

		_viewModel = viewModel;
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="INotifyPropertyChanged.PropertyChanged" /> event handler of <see cref="EditorViewModel" />.
	/// </summary>
	private void EditorViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (!string.Equals(e.PropertyName, nameof(EditorViewModel.IsReadOnly))
			|| sender is not EditorViewModel editor)
		{
			return;
		}

		IsReadOnly = editor.IsReadOnly;
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

		window.ViewModel.PropertyChanged += EditorViewModel_PropertyChanged;

		Disposable
			.Create(() => window.ViewModel.PropertyChanged -= EditorViewModel_PropertyChanged)
			.DisposeWith(_disposables);
	}

	/// <inheritdoc />
	protected override void AfterDispose()
	{
		base.AfterDispose();

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
	/// Tries to decrypt the content, if it has been decrypted.
	/// </summary>
	protected byte[]? TryToDecrypt(byte[] input)
	{
		if (SessionEncryptedDek is null || input.IsEmpty())
		{
			return input;
		}

		return _entityEcryption.DecryptSessionContents(
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

		return _entityEcryption.EncryptSessionContents(
			input,
			SessionEncryptedDek);
	}
	#endregion
}
