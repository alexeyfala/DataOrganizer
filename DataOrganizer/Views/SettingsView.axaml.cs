using DataOrganizer.ViewModels;
using DataOrganizer.Wrappers;

namespace DataOrganizer.Views;

public partial class SettingsView : CustomUserControl
{
	#region Constructors
	public SettingsView(SettingsViewModel viewModel)
	{
		InitializeComponent();

		DataContext = viewModel;
	}
	#endregion
}