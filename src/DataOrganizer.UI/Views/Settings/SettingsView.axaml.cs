using DataOrganizer.UI.ViewModels.Dialogs;
using DataOrganizer.UI.Views.Dialogs;

namespace DataOrganizer.UI.Views.Settings;

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
