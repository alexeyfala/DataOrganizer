using DataOrganizer.Enums.Documents;
using Hierarchy.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Clipboard;

/// <summary>
/// Places the text of a file on the clipboard, as a sensitive one when the file is encrypted.
/// </summary>
public interface IFileTextCopier
{
	#region Methods
	/// <summary>
	/// Copies the text of a file; a locked keeper is asked for its password under <paramref name="header" />. Returns
	/// what the contents hold, and only a text is handed to the clipboard.
	/// </summary>
	Task<FileTextStatus> CopyAsync(
		FileDto file,
		string header,
		CancellationToken token = default);
	#endregion
}
