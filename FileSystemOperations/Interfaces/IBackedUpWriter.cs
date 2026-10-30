using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileSystemOperations.Interfaces;

/// <summary>
/// Runs a write of several steps over a copy of the database, which the database goes back to when the write fails.
/// </summary>
public interface IBackedUpWriter
{
	#region Methods
	/// <summary>
	/// Copies the database and runs <paramref name="write" />; a write that answers <c>false</c> or throws brings the
	/// database back to the copy, cancelled or not, and the exception goes on. <c>False</c> also when no copy could be
	/// made, which is told.
	/// </summary>
	Task<bool> WriteAsync(Func<Task<bool>> write, CancellationToken token = default);
	#endregion
}
