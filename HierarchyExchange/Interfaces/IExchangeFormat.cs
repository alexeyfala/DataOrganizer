using Repository.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace HierarchyExchange.Interfaces;

/// <summary>
/// A file format the objects of the database are exported to and imported from.
/// </summary>
public interface IExchangeFormat
{
	#region Properties
	/// <summary>
	/// The extension of the files of the format, with the leading dot.
	/// </summary>
	string Extension { get; }

	/// <summary>
	/// The name of the file type in the file picker.
	/// </summary>
	string FileTypeName { get; }

	/// <summary>
	/// <c>True</c> when a file is a copy of the whole database, dates included, which a replacing import puts in place of
	/// the database.
	/// </summary>
	bool IsDatabaseCopy { get; }

	/// <summary>
	/// The MIME type of the files of the format.
	/// </summary>
	string MimeType { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Writes every object of the database to <paramref name="filePath" />.
	/// </summary>
	Task ExportAsync(string filePath, CancellationToken token = default);

	/// <summary>
	/// Reads the folders and files of <paramref name="filePath" />; <c>null</c> when the file holds none that can be read.
	/// </summary>
	Task<LoadedEntities?> ReadAsync(string filePath, CancellationToken token = default);
	#endregion
}
