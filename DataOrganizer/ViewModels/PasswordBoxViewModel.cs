using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.Views;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="PasswordBox" />.
/// </summary>
public sealed partial class PasswordBoxViewModel : AsyncResultViewModelBase<bool>
{
	#region Auto-Generated Properties
	/// <summary>
	/// Password.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
	private string? _password;
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Cancel.
	/// </summary>
	[RelayCommand]
	private Task Cancel() => SetResultAsync(false);

	/// <summary>
	/// Save.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteSave))]
	private Task Save() => SetResultAsync(true);
	#endregion

	#region Methods
	/// <inheritdoc cref="AsyncResultViewModelBase{TResult}.GetResultAsync" />
	public Task<bool> GetResultAsync(CancellationToken token = default)
	{
		return GetResultAsync(defaultResult: false, token);
	}
	#endregion

	#region Service
	/// <summary>
	/// Validates <see cref="SaveCommand" />.
	/// </summary>
	private bool CanExecuteSave()
	{
		const char space = ' ';

		return !string.IsNullOrWhiteSpace(Password)
			&& !Password.StartsWith(space)
			&& !Password.EndsWith(space);
	}
	#endregion
}
