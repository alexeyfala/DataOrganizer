using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using Repository.DTO;
using System.Collections.ObjectModel;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>PropertiesView</c>.
/// </summary>
internal sealed partial class PropertiesViewModel
{
	#region Properties
	/// <summary>
	/// The list of properties.
	/// </summary>
	public ObservableCollection<PropertyNameValuePair> Properties { get; } = [];
	#endregion Properties

	#region Auto-Generated Commands
	/// <summary>
	/// Closes the dialog.
	/// </summary>
	[RelayCommand]
	private static void Close() => DialogHost.Close(null);
	#endregion
}
