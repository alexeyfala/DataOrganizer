using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Abstract;
using DataOrganizer.Views;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="PasswordBox" />.
/// </summary>
public sealed partial class PasswordBoxViewModel : DefaultButtonViewModelBase
{
	#region Auto-Generated Properties
	/// <summary>
	/// Password.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(DefaultPressedCommand))]
	private string? _password;
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override bool CanExecuteDefaultPressed()
	{
		const char space = ' ';

		return !string.IsNullOrWhiteSpace(Password)
			&& !Password.StartsWith(space)
			&& !Password.EndsWith(space);
	}
	#endregion
}
