using FileContents.Dto;
using Hierarchy.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace FileContents.Interfaces;

/// <summary>
/// Reads the text of a file: decodes the contents <see cref="IFileContentsReader" /> opens, then wipes the decrypted bytes.
/// </summary>
public interface IFileTextReader
{
	#region Methods
	/// <summary>
	/// Reads the text of a file; a locked keeper is asked for its password under <paramref name="header" />.
	/// </summary>
	Task<FileTextReading> ReadAsync(
		FileDto file,
		string header,
		CancellationToken token = default);

	/// <summary>
	/// Reads the text of a file without asking for a password: the contents under a locked keeper stay closed.
	/// </summary>
	Task<FileTextReading> ReadUnlockedAsync(FileDto file, CancellationToken token = default);
	#endregion
}
