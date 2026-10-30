using Hierarchy.Dto;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FileExecution.Interfaces;

/// <summary>
/// Closes files open in the editor or in the operating system.
/// </summary>
public interface IOpenFilesCloser
{
	#region Methods
	/// <summary>
	/// Closes the open files without a question.
	/// </summary>
	void Close(IReadOnlyCollection<FileDto> openedFiles);

	/// <summary>
	/// Asks to close the open files and closes them; <c>false</c> when the user refuses, and nothing is closed then. No
	/// open files need no question.
	/// </summary>
	Task<bool> TryCloseAsync(IReadOnlyCollection<FileDto> openedFiles, CancellationToken token = default);
	#endregion
}
