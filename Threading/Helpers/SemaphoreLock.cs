using System;
using System.Threading;

namespace Threading.Helpers;

/// <summary>
/// A held semaphore that its disposal gives back, also when the semaphore has been disposed meanwhile; without a
/// semaphore, a wait that held nothing.
/// </summary>
public readonly record struct SemaphoreLock(SemaphoreSlim? Semaphore) : IDisposable
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the semaphore is held.
	/// </summary>
	public bool IsHeld => Semaphore is not null;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose()
	{
		try
		{
			Semaphore?.Release();
		}
		catch (ObjectDisposedException)
		{
			// The owner was disposed concurrently, and nobody waits for the semaphore any more.
		}
	}
	#endregion
}
