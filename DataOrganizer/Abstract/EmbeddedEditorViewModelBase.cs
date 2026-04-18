using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using Entities.Models;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Abstract;

public abstract partial class EmbeddedEditorViewModelBase : ObservableDisposable
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

	#region Data
	/// <inheritdoc cref="IDbAccess" />
	protected readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IJsonSerializerWrapper" />
	protected readonly IJsonSerializerWrapper _jsonSerializer;

	/// <inheritdoc cref="ILogger" />
	protected readonly ILogger _logger;

	/// <inheritdoc cref="Lock" />
	protected readonly Lock _mutex = new();

	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IDispatcher" />
	private readonly IDispatcher _dispatcher;

	/// <inheritdoc cref="IEntityEcryption" />
	private readonly IEntityEcryption _entityEcryption;
	#endregion

	#region Constructors
	protected EmbeddedEditorViewModelBase(
		Application app,
		IDbAccess dbAccess,
		IDispatcher dispatcher,
		IEntityEcryption entityEcryption,
		IJsonSerializerWrapper jsonSerializer,
		ILogger logger)
	{
		_app = app;

		_dbAccess = dbAccess;

		_dispatcher = dispatcher;

		_entityEcryption = entityEcryption;

		_jsonSerializer = jsonSerializer;

		_logger = logger;
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="INotifyPropertyChanged.PropertyChanged" /> event handler of <see cref="EditorViewModel" />.
	/// </summary>
	private void EditorViewModel_PropertyChanged(EventPattern<PropertyChangedEventArgs> e)
	{
		if (!string.Equals(e.EventArgs.PropertyName, nameof(EditorViewModel.IsReadOnly))
			|| e.Sender is not EditorViewModel editor)
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

		Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
			x => window.ViewModel.PropertyChanged += x,
			x => window.ViewModel.PropertyChanged -= x)
			.Subscribe(EditorViewModel_PropertyChanged)
			.DisposeWith(_disposables);
	}

	/// <summary>
	/// Displays object in the list.
	/// </summary>
	protected static Task ShowInListAsync(Window? window, Guid fileId)
	{
		if (window?.DataContext is not ViewModelBase viewModel)
		{
			return Task.CompletedTask;
		}

		return viewModel.ShowInEditorAsync(window, fileId);
	}

	/// <inheritdoc />
	protected override void AfterDispose()
	{
		SessionEncryptedDek?.ZeroMemory();

		SessionEncryptedDek = null;
	}

	/// <summary>
	/// Saves <see cref="FileModel.Contents" /> to the database.
	/// </summary>
	protected Task SaveContentsAsync(byte[] contents, CancellationToken token = default)
	{
		_logger.LogDebug($@"Saving contents of ""{FileId}"" in the database");

		return UpdatePropertyAsync(
			propertyName: nameof(FileModel.Contents),
			value: contents,
			isUpdatedDate: true,
			token: token);
	}

	/// <summary>
	/// Saves <see cref="FileModel.Properties" /> to the database.
	/// </summary>
	protected Task SavePropertiesAsync(
		string json,
		CancellationToken token = default)
	{
		_logger.LogDebug(
			$@"Saving properties of ""{FileId}"" in the database:{json}");

		return UpdatePropertyAsync(
			propertyName: nameof(FileModel.Properties),
			value: json,
			isUpdatedDate: false,
			token: token);
	}

	/// <inheritdoc cref="ViewModelBase.ShowErrorSnackbar" />
	protected void ShowErrorSnackbar(StyledElement? element, string text)
	{
		_dispatcher.Post(() =>
		{
			if (element
				.FindLogicalParent<Window>()?
				.DataContext is not ViewModelBase viewModel)
			{
				return;
			}

			viewModel.ShowErrorSnackbar(text);
		});
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

	#region Service
	/// <summary>
	/// Updates property of <see cref="FileModel" /> in the database.
	/// </summary>
	private async Task UpdatePropertyAsync<T>(
		string propertyName,
		T value,
		bool isUpdatedDate,
		CancellationToken token) where T : notnull
	{
		DateTime updatedDate = DateTime.Now;

		PropertyNameValuePair[] properties =
		[
			new PropertyNameValuePair(propertyName, value)
		];

		if (isUpdatedDate)
		{
			properties = [.. properties, new(nameof(FileModel.UpdatedDate), updatedDate)];
		}

		if (!await _dbAccess.UpdatePropertiesAsync(
			id: FileId,
			token: token,
			properties).ConfigureAwait(false) || !isUpdatedDate)
		{
			return;
		}

		SetUpdatedDateCallback?.Invoke(updatedDate);
	}
	#endregion
}
