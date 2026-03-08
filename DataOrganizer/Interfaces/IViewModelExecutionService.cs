using DataOrganizer.Abstract;
using DataOrganizer.ViewModels;
using System;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides methods to execute code in view models of the application.
/// </summary>
public interface IViewModelExecutionService
{
	#region Methods
	/// <summary>
	/// Searches <see cref="ViewModelBase" /> in main thread and executes the action.
	/// </summary>
	void ExecuteInBaseViewModel(Action<ViewModelBase> action);

	/// <summary>
	/// Searches <see cref="EditorViewModel" /> in main thread and executes the action.
	/// </summary>
	void ExecuteInEditor(Action<EditorViewModel> action);
	#endregion
}
