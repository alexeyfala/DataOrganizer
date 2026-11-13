using DataOrganizer.Hierarchy.Dto;
using System.Threading.Tasks;

namespace DataOrganizer.Documents.FileContents.Interfaces;

/// <summary>
/// Reads the start of the text of a file for a preview.
/// </summary>
public interface IFilePreviewReader
{
	#region Methods
	/// <summary>
	/// Returns the start of the text of a file; <c>null</c> when the file has no text to show.
	/// </summary>
	Task<string?> ReadPreviewAsync(FileDto file);
	#endregion
}
