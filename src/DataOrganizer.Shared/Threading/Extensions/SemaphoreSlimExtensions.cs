using DataOrganizer.Shared.Threading.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Shared.Threading.Extensions;

public static class SemaphoreSlimExtensions
{
	#region Methods
	/// <summary>
	/// Waits for the semaphore and returns the lock that gives it back when disposed.
	/// </summary>
	public static async Task<SemaphoreLock> LockAsync(this SemaphoreSlim semaphore, CancellationToken token = default)
	{
		await semaphore
			.WaitAsync(token)
			.ConfigureAwait(false);

		return new(semaphore);
	}
	#endregion
}
