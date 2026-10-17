using DialogHostAvalonia;

namespace DataOrganizer.Interfaces.Dialogs;

/// <summary>
/// Finds the dialog hosts of the open windows.
/// </summary>
public interface IDialogHostFinder
{
	#region Methods
	/// <summary>
	/// Finds the dialog host with <paramref name="identifier" /> among the open windows.
	/// </summary>
	DialogHost? FindDialogHost(string identifier);
	#endregion
}
