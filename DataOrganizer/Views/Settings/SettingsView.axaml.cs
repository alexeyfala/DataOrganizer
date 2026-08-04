using DataOrganizer.ViewModels;
using DataOrganizer.Wrappers;

namespace DataOrganizer.Views.Settings;

public partial class SettingsView : DialogViewBase
{
	#region Constructors
	public SettingsView() => InitializeComponent();

	public SettingsView(SettingsViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
