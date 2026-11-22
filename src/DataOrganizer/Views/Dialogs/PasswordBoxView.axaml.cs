using Avalonia.Controls;
using DataOrganizer.ViewModels.Dialogs;

namespace DataOrganizer.Views.Dialogs;

/// <summary>
/// Dialog that asks for a password.
/// </summary>
public sealed partial class PasswordBoxView : UserControl
{
	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="PasswordBoxView" /> class.
	/// </summary>
	public PasswordBoxView() => InitializeComponent();

	/// <summary>
	/// Initializes a new instance of the <see cref="PasswordBoxView" /> class.
	/// </summary>
	public PasswordBoxView(PasswordBoxViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
