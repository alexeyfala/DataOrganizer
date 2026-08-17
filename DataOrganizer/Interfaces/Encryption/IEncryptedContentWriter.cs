using DataOrganizer.DTO.Encryption;
using DataOrganizer.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Persists processed contents and notes, restoring the database from the backup when a step fails.
/// </summary>
public interface IEncryptedContentWriter
{
	#region Methods
	/// <summary>
	/// Writes the processed contents, notes and the wrapped key, and applies the new status to the objects.
	/// </summary>
	Task<UpdateDatabaseResult> UpdateDatabaseAsync(
		UpdateDatabaseParameters parameters,
		CancellationToken token = default);
	#endregion
}
