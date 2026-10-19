using DataOrganizer.Interfaces.Documents;
using System;
using System.Collections.Generic;

namespace DataOrganizer.Services.Documents;

public sealed class EditorSplitStore : IEditorSplitStore
{
	#region Data
	/// <summary>
	/// Shares of the height of the upper half by the identifier of the file; a file that is not split has none.
	/// </summary>
	private readonly Dictionary<Guid, double> _splits = [];
	#endregion

	#region Methods
	/// <inheritdoc />
	public double? Find(Guid fileId) => _splits.TryGetValue(fileId, out double split) ? split : null;

	/// <inheritdoc />
	public void Set(Guid fileId, double? split)
	{
		if (split is not { } share)
		{
			_splits.Remove(fileId);

			return;
		}

		_splits[fileId] = share;
	}
	#endregion
}
