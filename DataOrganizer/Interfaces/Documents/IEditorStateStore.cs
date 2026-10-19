using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Documents;

/// <summary>
/// Reads and writes the state of the built-in editor of a file in the database.
/// </summary>
public interface IEditorStateStore
{
	#region Methods
	/// <summary>
	/// Returns the stored editor state of a file; <c>null</c> when it has none.
	/// </summary>
	Task<string?> ReadAsync(Guid fileId, CancellationToken token = default);

	/// <summary>
	/// Writes the editor state of a file.
	/// </summary>
	Task WriteAsync(
		Guid fileId,
		[StringSyntax(StringSyntaxAttribute.Json)] string json,
		CancellationToken token = default);
	#endregion
}
