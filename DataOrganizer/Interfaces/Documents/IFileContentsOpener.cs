using Entities.Enums;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Documents;

/// <summary>
/// Opens the contents of a file for use, and tells the user and the log why they cannot be opened.
/// </summary>
public interface IFileContentsOpener
{
	#region Methods
	/// <summary>
	/// Reads the contents of a file without asking for a password, opened by the session key of its keeper; <c>null</c>
	/// when they cannot be read or opened, which is told.
	/// </summary>
	Task<byte[]?> OpenAsync(
		Guid fileId,
		Guid? keeperId,
		string? fileName,
		EntityKind kind);
	#endregion
}
