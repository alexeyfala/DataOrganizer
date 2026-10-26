using DataOrganizer.Dto.Documents;
using Hierarchy.Dto;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Documents;

/// <summary>
/// Reads the stored contents of a file from the database: checks them and decrypts the protected ones.
/// </summary>
public interface IFileContentsReader
{
	#region Methods
	/// <summary>
	/// Reads the contents of a file; a locked keeper is asked for its password under <paramref name="header" />.
	/// </summary>
	Task<FileContentsReading> ReadAsync(
		FileDto file,
		string header,
		CancellationToken token = default);

	/// <summary>
	/// Reads the contents of a file without asking for a password: the contents under a locked keeper stay closed.
	/// </summary>
	Task<FileContentsReading> ReadUnlockedAsync(FileDto file, CancellationToken token = default);

	/// <summary>
	/// Reads the contents of a file without asking for a password, opening them with the session key of the keeper when
	/// one is given.
	/// </summary>
	Task<FileContentsReading> ReadUnlockedAsync(
		Guid fileId,
		Guid? keeperId,
		CancellationToken token = default);
	#endregion
}
