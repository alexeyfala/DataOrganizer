using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.Views;
using Shared.Properties;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="PasswordBox" />.
/// </summary>
public sealed partial class PasswordBoxViewModel : BooleanAsyncResultViewModelBase
{
	#region Auto-Generated Properties
	/// <summary>
	/// Header.
	/// </summary>
	[ObservableProperty]
	private string? _header;

	/// <summary>
	/// The label.
	/// </summary>
	[ObservableProperty]
	private string _label = Strings.Password;

	/// <summary>
	/// Password.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
	private string? _password;
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Apply.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteApply))]
	private Task Apply() => SetResultAsync(true);

	/// <summary>
	/// Cancel.
	/// </summary>
	[RelayCommand]
	private Task Cancel() => SetResultAsync(false);
	#endregion

	#region Constructors
	public PasswordBoxViewModel(Application app) : base(app)
	{
	}
	#endregion

	#region Service
	/// <summary>
	/// Validates <see cref="ApplyCommand" />.
	/// </summary>
	private bool CanExecuteApply()
	{
		const char space = ' ';

		return !string.IsNullOrWhiteSpace(Password)
			&& !Password.StartsWith(space)
			&& !Password.EndsWith(space);
	}
	#endregion
}
