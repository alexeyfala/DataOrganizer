using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Execution;

/// <summary>
/// Folder holding the decrypted copies of the files opened in external applications.
/// </summary>
public interface ISandbox
{
	#region Methods
	/// <summary>
	/// Overwrites the contents of the folder and deletes it, repeating the attempt while the files stay locked.
	/// </summary>
	Task EraseAsync(CancellationToken token = default);

	/// <summary>
	/// Returns the path of the folder holding the copy of the given file.
	/// </summary>
	string GetFileDirectoryPath(Guid fileId);
	#endregion
}
