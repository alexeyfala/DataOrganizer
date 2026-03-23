using DataOrganizer.DTO;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides methods for opening files in different OS.
/// </summary>
public interface IExecutionEngine : IDisposable
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
	/// Returns <c>True</c> if the file is already executed in the operating system.
	/// </summary>
	bool IsExecuted(Guid id);
	#endregion
}
