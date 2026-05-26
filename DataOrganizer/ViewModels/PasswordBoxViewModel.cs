using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="PasswordBox" />.
/// </summary>
public sealed partial class PasswordBoxViewModel : BooleanAsyncResultViewModel
{
	#region Properties
	/// <summary>
	/// Dialog header.
	/// </summary>
	[ObservableProperty]
	public partial string? Header { get; set; }

	/// <summary>
	/// Validity flag driven by the view's code-behind so the password string itself
	/// is never bound into a managed property on this view model.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
	public partial bool IsPasswordValid { get; set; }

	/// <summary>
	/// Floating placeholder shown above the password input.
	/// </summary>
	[ObservableProperty]
	public partial string? Label { get; set; }
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Apply.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanApply))]
	private Task Apply() => SetResultAsync(true);

	/// <summary>
	/// Cancel.
	/// </summary>
	[RelayCommand]
	private Task Cancel() => SetResultAsync(false);

	/// <summary>
	/// Computes <see cref="IsPasswordValid" /> from the current password text passed by the view.
	/// </summary>
	[RelayCommand]
	private void ValidatePassword(string? value)
	{
		const char space = ' ';

		IsPasswordValid = !string.IsNullOrWhiteSpace(value)
			&& !value.StartsWith(space)
			&& !value.EndsWith(space);
	}
	#endregion

	#region Constructors
	public PasswordBoxViewModel(
		Application app,
		ITaskExceptionHandler handler) : base(app, handler)
	{
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="ApplyCommand" />.
	/// </summary>
	private bool CanApply() => IsPasswordValid;
	#endregion
}
