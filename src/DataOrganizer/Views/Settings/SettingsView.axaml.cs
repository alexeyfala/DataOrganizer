using DataOrganizer.ViewModels.Dialogs;
using DataOrganizer.Views.Dialogs;

namespace DataOrganizer.Views.Settings;

/// <summary>
/// Dialog of the application settings.
/// </summary>
public partial class SettingsView : DialogViewBase
{
	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="SettingsView" /> class.
	/// </summary>
	public SettingsView() => InitializeComponent();

	/// <summary>
	/// Initializes a new instance of the <see cref="SettingsView" /> class.
	/// </summary>
	public SettingsView(SettingsViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
