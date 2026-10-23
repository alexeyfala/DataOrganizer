using DataOrganizer.Helpers.Security;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Writes the wrapper of a data encryption key again at the current derivation cost.
/// </summary>
public interface IDekRewrapper
{
	#region Methods
	/// <summary>
	/// Hands a new wrapper of an outdated one to the write. The DEK itself does not change, so a failure
	/// is only logged and leaves a wrapper the same password still opens.
	/// </summary>
	Task RewrapAsync(
		byte[] wrapped,
		PinnedBuffer dek,
		PinnedBuffer password,
		ContentIdentity identity,
		Func<byte[], CancellationToken, Task> write,
		CancellationToken token = default);
	#endregion
}
