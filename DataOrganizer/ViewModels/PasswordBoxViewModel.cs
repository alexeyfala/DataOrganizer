using Avalonia;
using DataOrganizer.Abstract;
using DataOrganizer.Views;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="PasswordBox" />.
/// </summary>
public sealed class PasswordBoxViewModel : BooleanAsyncResultViewModelBase
{
	#region Constructors
	public PasswordBoxViewModel(Application app) : base(app)
	{
	}
	#endregion
}
