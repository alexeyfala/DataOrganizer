using Repository.DTO;
using System.Collections.ObjectModel;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>PropertiesView</c>.
/// </summary>
internal sealed class PropertiesViewModel
{
	#region Properties
	/// <summary>
	/// The list of properties.
	/// </summary>
	public ObservableCollection<PropertyNameValuePair> Properties { get; } = [];
	#endregion Properties
}
