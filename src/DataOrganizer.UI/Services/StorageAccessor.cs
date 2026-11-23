using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DataOrganizer.UI.Interfaces;
using DataOrganizer.UI.Interfaces.Views;
using System.Threading.Tasks;

namespace DataOrganizer.UI.Services;

/// <inheritdoc cref="IStorageAccessor" />
public sealed class StorageAccessor : IStorageAccessor
{
	#region Data
	/// <inheritdoc cref="IWindowFinder" />
	private readonly IWindowFinder _windowFinder;
	#endregion

	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="StorageAccessor" /> class.
	/// </summary>
	public StorageAccessor(IWindowFinder windowFinder) => _windowFinder = windowFinder;
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task<IStorageFile?> TryGetFileFromPathAsync(string filePath)
	{
		if (FindStorageProvider() is not { } provider)
		{
			return Task.FromResult(default(IStorageFile));
		}

		return provider.TryGetFileFromPathAsync(filePath);
	}

	/// <inheritdoc />
	public Task<IStorageFolder?> TryGetFolderFromPathAsync(string folderPath)
	{
		if (FindStorageProvider() is not { } provider)
		{
			return Task.FromResult(default(IStorageFolder));
		}

		return provider.TryGetFolderFromPathAsync(folderPath);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Finds the storage of the first open window.
	/// </summary>
	private IStorageProvider? FindStorageProvider() => _windowFinder.FindWindow<Window>()?.StorageProvider;
	#endregion
}
