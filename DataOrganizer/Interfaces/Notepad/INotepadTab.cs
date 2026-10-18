using DataOrganizer.Interfaces.Documents;

namespace DataOrganizer.Interfaces.Notepad;

/// <summary>
/// Tab of the notepad whose text is kept on the disk.
/// </summary>
public interface INotepadTab
{
	#region Properties
	/// <summary>
	/// Text of the tab with its encoding.
	/// </summary>
	IEncodedDocument Editor { get; }

	/// <summary>
	/// Name of the tab as shown.
	/// </summary>
	string Header { get; }

	/// <summary>
	/// <c>True</c> when the text cannot be read from the disk, so it is neither edited nor written back.
	/// </summary>
	bool IsReadOnly { get; set; }

	/// <summary>
	/// Number of the tab, unique among the open tabs.
	/// </summary>
	int Number { get; }
	#endregion
}
