using EditorDocuments.Dto;

namespace AppInstance.Interfaces;

/// <summary>
/// Tabs of the editor that the work session keeps while another main window is shown.
/// </summary>
public interface ISessionEditorTabs
{
	#region Properties
	/// <summary>
	/// Tabs the editor opens when it is shown again; <c>null</c> stands for none.
	/// </summary>
	EditorTabsState? EditorTabs { get; set; }
	#endregion
}
