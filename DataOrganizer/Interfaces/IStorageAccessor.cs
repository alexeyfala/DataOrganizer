using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides means to interact with Avalonia's storage provider.
/// </summary>
public interface IStorageAccessor
{
	#region Methods
	/// <summary>
	/// Attempts to read file from the file-system by its path.
	/// </summary>
	Task<IStorageFile?> TryGetFileFromPathAsync(string filePath);

	/// <summary>
	/// Attempts to read folder from the file-system by its path.
	/// </summary>
	Task<IStorageFolder?> TryGetFolderFromPathAsync(string folderPath);
	#endregion
}
