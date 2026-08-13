using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Interfaces;
using Shared.Properties;
using System.Globalization;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>PasswordBox</c>.
/// </summary>
public sealed partial class PasswordBoxViewModel : BooleanAsyncResultViewModel
{
	#region Data
	/// <summary>
	/// Least number of characters a new password is accepted with.
	/// </summary>
	public const int MinimumPasswordLength = 8;
	#endregion

	#region Properties
	/// <summary>
	/// Assistive text under the confirmation input; <c>null</c> while the two inputs agree.
	/// </summary>
	public string? ConfirmationHint => IsConfirmationMismatched
		? Strings.PasswordsDoNotMatch
		: null;

	/// <summary>
	/// Dialog header.
	/// </summary>
	[ObservableProperty]
	public partial string? Header { get; set; }

	/// <summary>
	/// <c>True</c> while the confirmation input holds something other than the password.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ConfirmationHint))]
	public partial bool IsConfirmationMismatched { get; set; }

	/// <summary>
	/// <c>True</c> when a new password is being set, so it is confirmed in a second input.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(PasswordHint))]
	public partial bool IsConfirmationVisible { get; set; }

	/// <summary>
	/// <c>True</c> while the password input holds fewer characters than the policy asks for.
	/// </summary>
	[ObservableProperty]
	public partial bool IsPasswordTooShort { get; set; }

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

	/// <summary>
	/// Assistive text under the password input; <c>null</c> while an existing password is entered.
	/// </summary>
	public string? PasswordHint => IsConfirmationVisible
		? string.Format(CultureInfo.CurrentCulture, Strings.PasswordMinimumLength, MinimumPasswordLength)
		: null;
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
	#endregion

	#region Constructors
	public PasswordBoxViewModel(
		Application app,
		ITaskExceptionHandler exceptionHandler) : base(app, exceptionHandler)
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
