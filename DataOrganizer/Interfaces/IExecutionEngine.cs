using DataOrganizer.DTO.Execution;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides methods for opening files in different OS.
/// </summary>
public interface IExecutionEngine : IAsyncDisposable
{
	#region Methods
	/// <summary>
	/// Closes a file in the operating system.
	/// </summary>
	Task CloseAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// Executes a file in the operating system.
	/// </summary>
	Task<bool> ExecuteAsync(ExecuteFileParameters parameters, CancellationToken token = default);

	/// <summary>
	/// <c>True</c> when the file is already executed by the operating system.
	/// </summary>
	bool IsExecuting(Guid id);
	#endregion
}
