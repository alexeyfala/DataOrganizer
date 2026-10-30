using DataProtection.Dto;
using DataProtection.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace DataProtection.Interfaces;

/// <summary>
/// Persists processed contents and notes, and tells when they could not be written.
/// </summary>
public interface IEncryptedContentWriter
{
	#region Methods
	/// <summary>
	/// Writes the processed contents, notes and the wrapped key, and applies the new status to the objects.
	/// </summary>
	Task<UpdateDatabaseOutcome> UpdateDatabaseAsync(
		UpdateDatabaseParameters parameters,
		CancellationToken token = default);
	#endregion
}
