using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.Abstract;

/// <summary>
/// A base ViewModel with <see cref="DefaultPressedCallback" /> property.
/// </summary>
public abstract partial class DefaultButtonViewModelBase : ObservableObject
{
	#region Properties
	/// <summary>
	/// A reference to the method called when the default button is pressed.
	/// </summary>
	public Func<Task>? DefaultPressedCallback { get; set; }

	/// <summary>
	/// Returns <c>True</c> if initialization has completed.
	/// </summary>
	/// <remarks>
	/// For test purposes.
	/// </remarks>
	public bool IsInitialized { get; protected set; }
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Default button pressed command.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteDefaultPressed))]
	private void DefaultPressed()
	{
		DefaultPressedCallback?.Invoke();

		DefaultPressedCallback = null;

		AfterDefaultPressed();
	}
	#endregion

	#region Methods
	/// <summary>
	/// Called after main <see cref="DefaultPressed" /> method.
	/// </summary>
	protected virtual void AfterDefaultPressed()
	{
	}

	/// <summary>
	/// Validates <see cref="DefaultPressedCommand" />.
	/// </summary>
	protected abstract bool CanExecuteDefaultPressed();
	#endregion
}
