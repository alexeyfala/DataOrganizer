using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using Repository.Interfaces;
using Serilog;
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
	#region Auto-Generated Properties
	/// <summary>
	/// Read-only mode.
	/// </summary>
	[ObservableProperty]
	private bool _isReadOnly;
	#endregion

	#region Data
	/// <inheritdoc cref="Application" />
	protected readonly Application _app;

	/// <inheritdoc cref="IDbAccess" />
	protected readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDispatcher" />
	protected readonly IDispatcher _dispatcher;

	/// <inheritdoc cref="IEncryptionService" />
	protected readonly IEncryptionService _encryption;

	/// <inheritdoc cref="IEntityEcryption" />
	protected readonly IEntityEcryption _entityEcryption;

	/// <inheritdoc cref="IJsonSerializerWrapper" />
	protected readonly IJsonSerializerWrapper _jsonSerializer;

	/// <inheritdoc cref="ILogger" />
	protected readonly ILogger _logger;

	/// <inheritdoc cref="Lock" />
	protected readonly Lock _mutex = new();
	#endregion

	#region Constructors
	protected EmbeddedEditorViewModelBase(
		Application app,
		IDbAccess dbAccess,
		IDispatcher dispatcher,
		IEncryptionService encryption,
		IEntityEcryption entityEcryption,
		IJsonSerializerWrapper jsonSerializer,
		ILogger logger)
	{
		_app = app;

		_dbAccess = dbAccess;

		_dispatcher = dispatcher;

		_encryption = encryption;

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
	/// <inheritdoc cref="IFileEditor.Initialize" />
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

	/// <inheritdoc cref="ViewModelBase.ShowErrorSnackbar" />
	protected static void ShowErrorSnackbar(Window? window, string text)
	{
		if (window?.DataContext is not ViewModelBase viewModel)
		{
			return;
		}

		viewModel.ShowErrorSnackbar(text);
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
	#endregion
}
