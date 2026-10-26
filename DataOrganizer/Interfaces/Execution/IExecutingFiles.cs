using DataOrganizer.Dto.Execution;
using Hierarchy.Dto;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Execution;

/// <summary>
/// Files the operating system runs for the work session: the only place that starts and closes them and marks them.
/// </summary>
public interface IExecutingFiles
{
	#region Properties
	/// <summary>
	/// Files executing in the operating system, in the order they started.
	/// </summary>
	ReadOnlyObservableCollection<FileDto> Files { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Closes a file executing in the operating system.
	/// </summary>
	void Close(FileDto file);

	/// <summary>
	/// Closes every file the operating system runs and waits for it, leaving the list and the marks as they are.
	/// </summary>
	Task CloseAllAsync();

	/// <summary>
	/// Runs a file in the operating system; <c>false</c> when it does not run.
	/// </summary>
	Task<bool> ExecuteAsync(ExecuteFileParameters parameters, CancellationToken token = default);

	/// <summary>
	/// <c>True</c> when the operating system already runs the file.
	/// </summary>
	bool IsExecuting(Guid id);
	#endregion
}
