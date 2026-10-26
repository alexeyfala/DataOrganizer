using Hierarchy.Dto;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Execution;

/// <summary>
/// Runs a file of the tree in the operating system: first the checks and the question, then the read of its contents
/// and the run; a protected file is opened between the two.
/// </summary>
public interface IFileExecutionScenario
{
	#region Methods
	/// <summary>
	/// <c>True</c> when the file may run: it is not running yet, and the user agrees to run a file that may start a
	/// program or a script.
	/// </summary>
	Task<bool> ConfirmAsync(FileDto file);

	/// <summary>
	/// Reads the contents of the file, opened with the session key of its keeper when protected, and runs it in the
	/// operating system; <paramref name="isReadOnly" /> keeps its changes out of the database.
	/// </summary>
	Task ExecuteAsync(FileDto file, bool isReadOnly);
	#endregion
}
