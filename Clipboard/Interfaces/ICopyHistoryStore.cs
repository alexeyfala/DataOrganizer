using Hierarchy.Dto;
using System.Collections.Generic;

namespace Clipboard.Interfaces;

/// <summary>
/// The file that keeps the copy history between the runs of the application.
/// </summary>
public interface ICopyHistoryStore
{
	#region Methods
	/// <summary>
	/// Reads the history from its file, keeping the files that <paramref name="hierarchy" /> holds.
	/// </summary>
	void Load(IEnumerable<ExplorerItemDtoBase> hierarchy);

	/// <summary>
	/// Writes the history over its file.
	/// </summary>
	void Save();
	#endregion
}
