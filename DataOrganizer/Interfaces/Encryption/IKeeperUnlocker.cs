using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Prompts for a password and unwraps the data encryption key of a password keeper.
/// </summary>
public interface IKeeperUnlocker
{
	#region Methods
	/// <summary>
	/// Prompts for the password and unwraps the DEK bound to the keeper; the caller owns the key.
	/// </summary>
	/// <returns>
	/// The unwrapped DEK, or <c>null</c> when the prompt is cancelled or the password is rejected;
	/// a rejection is reported to the user.
	/// </returns>
	Task<byte[]?> RequestDekAsync(
		Guid keeperId,
		byte[] encryptedDek,
		string header,
		string? label = null,
		CancellationToken token = default,
		[CallerMemberName] string callerName = "");
	#endregion
}
