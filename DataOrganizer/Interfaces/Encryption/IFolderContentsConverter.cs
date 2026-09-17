using DataOrganizer.Dto.Encryption;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Reads the contents of the files of a folder and converts them, and the notes, with a key.
/// </summary>
public interface IFolderContentsConverter
{
	#region Methods
	/// <summary>
	/// Converts the contents and the notes. Returns <c>Null</c> after reporting contents that cannot
	/// be read or converted, leaving nothing of them in plain text; the buffers of a returned
	/// conversion are erased by whoever receives it.
	/// </summary>
	Task<FolderConversion?> ConvertAsync(
		FolderConversionParameters parameters,
		CancellationToken token = default);
	#endregion
}
