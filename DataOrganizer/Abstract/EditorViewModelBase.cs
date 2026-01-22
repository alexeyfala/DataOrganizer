using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Abstract;

public abstract partial class EditorViewModelBase : ObservableDisposable
{
	#region Auto-Generated Properties
	/// <summary>
	/// Read-only mode.
	/// </summary>
	[ObservableProperty]
	private bool _isReadOnly;
	#endregion

	#region Data
	/// <inheritdoc cref="App" />
	protected readonly App _app;

	/// <inheritdoc cref="Lock" />
	protected readonly Lock _mutex = new();
	#endregion

	#region Constructors
	protected EditorViewModelBase(App app) => _app = app;
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
