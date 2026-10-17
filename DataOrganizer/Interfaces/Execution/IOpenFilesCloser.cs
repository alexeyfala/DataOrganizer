using DataOrganizer.Dto.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Execution;

/// <summary>
/// Closes files open in the editor or in the operating system once the user agrees.
/// </summary>
public interface IOpenFilesCloser
{
	#region Methods
	/// <summary>
	/// Asks to close the open files and closes them; <c>false</c> when the user refuses, and nothing is closed then. No
	/// open files need no question.
	/// </summary>
	Task<bool> TryCloseAsync(IReadOnlyCollection<FileDto> openedFiles, CancellationToken token = default);
	#endregion
}
