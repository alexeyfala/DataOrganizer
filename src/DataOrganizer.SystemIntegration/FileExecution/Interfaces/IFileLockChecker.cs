using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.SystemIntegration.FileExecution.Interfaces;

/// <summary>
/// Tells whether another process holds a file and waits for it to let the file go.
/// </summary>
public interface IFileLockChecker
{
	#region Methods
	/// <summary>
	/// <c>True</c> when the file is locked by another process.
	/// </summary>
	bool IsLocked(string filePath);

	/// <summary>
	/// Waits until the file is no longer locked or no longer exists; <c>False</c> when <paramref name="token" /> is
	/// cancelled while the file is still locked. Never throws on cancellation.
	/// </summary>
	ValueTask<bool> WaitUntilUnlockedAsync(string filePath, CancellationToken token = default);
	#endregion
}
