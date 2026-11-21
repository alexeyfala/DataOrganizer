using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Workflows.HierarchyExchange.Interfaces;

/// <summary>
/// An exchange format whose file can also hold only some of the objects of the database.
/// </summary>
public interface IPartialExportFormat : IExchangeFormat
{
	#region Methods
	/// <summary>
	/// Writes the objects of the database whose identifiers are in <paramref name="ids" /> to <paramref name="filePath" />.
	/// </summary>
	Task ExportPartAsync(
		string filePath,
		IReadOnlySet<Guid> ids,
		CancellationToken token = default);
	#endregion
}
