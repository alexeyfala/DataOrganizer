using DataOrganizer.ViewModels;
using DataOrganizer.Wrappers;

namespace DataOrganizer.Views;

public partial class SettingsView : CustomUserControl
{
	#region Properties
	/// <inheritdoc cref="SettingsViewModel" />
	public SettingsViewModel ViewModel { get; }
	#endregion

	#region Constructors
	public SettingsView(SettingsViewModel viewModel)
	{
		InitializeComponent();

		DataContext = ViewModel = viewModel;
	}
	#endregion
}