using System;

namespace EditorDocuments.Interfaces;

/// <summary>
/// Splits of the files in the built-in editor, kept while the application runs and never stored.
/// </summary>
public interface IEditorSplitStore
{
	#region Methods
	/// <summary>
	/// Returns the split of a file: the share of the height of the upper half, or <c>null</c> when it is not split.
	/// </summary>
	double? Find(Guid fileId);

	/// <summary>
	/// Keeps the split of a file: the share of the height of the upper half, or <c>null</c> when it is not split.
	/// </summary>
	void Set(Guid fileId, double? split);
	#endregion
}
