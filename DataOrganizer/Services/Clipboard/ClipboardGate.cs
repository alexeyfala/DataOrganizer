using DataOrganizer.Interfaces.Clipboard;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Clipboard;

public sealed class ClipboardGate : IClipboardGate, IDisposable
{
	#region Data
	/// <summary>
	/// The single-permit semaphore backing the gate.
	/// </summary>
	private readonly SemaphoreSlim _semaphore = new(1, 1);
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose() => _semaphore.Dispose();

	/// <inheritdoc />
	public void Release() => _semaphore.Release();

	/// <inheritdoc />
	public Task WaitAsync() => _semaphore.WaitAsync();
	#endregion
}
