using DataOrganizer.ViewModels;
using DataOrganizer.Wrappers;

namespace DataOrganizer.Views;

public partial class SettingsView : CustomUserControl
{
	#region Constructors
	public SettingsView() => InitializeComponent();

	public SettingsView(SettingsViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
