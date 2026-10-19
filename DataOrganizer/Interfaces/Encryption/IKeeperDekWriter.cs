using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Replaces the wrapper of the data encryption key of a password keeper, in the database and in the keeper.
/// </summary>
public interface IKeeperDekWriter
{
	#region Methods
	/// <summary>
	/// Writes the new wrapper; <c>false</c> when the database refuses it, and the keeper keeps the old one then.
	/// </summary>
	Task<bool> WriteAsync(
		IPasswordKeeper keeper,
		byte[] wrappedDek,
		CancellationToken token = default);
	#endregion
}
