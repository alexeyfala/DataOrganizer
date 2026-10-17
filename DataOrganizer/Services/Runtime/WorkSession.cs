using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces.Runtime;
using System.Collections.ObjectModel;

namespace DataOrganizer.Services.Runtime;

/// <summary>
/// State of the work that outlives the switch between the main windows and is never written to the settings files.
/// </summary>
internal sealed class WorkSession : ISessionEditorTabs, ISessionHierarchy, ISessionReadOnlyMode
{
	#region Properties
	/// <inheritdoc />
	public EditorTabsState? EditorTabs { get; set; }

	/// <inheritdoc />
	public ObservableCollection<ExplorerItemDtoBase> Hierarchy { get; } = [];

	/// <inheritdoc />
	public bool IsReadOnly { get; set; }
	#endregion
}
