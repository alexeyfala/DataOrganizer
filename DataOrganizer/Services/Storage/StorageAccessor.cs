using Avalonia;
using Avalonia.Platform.Storage;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces.Storage;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Storage;

public sealed class StorageAccessor : IStorageAccessor
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;
	#endregion

	#region Constructors
	public StorageAccessor(Application app) => _app = app;
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task<IStorageFile?> TryGetFileFromPathAsync(string filePath)
	{
		if (_app.FindStorageProvider() is not { } provider)
		{
			return Task.FromResult(default(IStorageFile));
		}

		return provider.TryGetFileFromPathAsync(filePath);
	}

	/// <inheritdoc />
	public Task<IStorageFolder?> TryGetFolderFromPathAsync(string folderPath)
	{
		if (_app.FindStorageProvider() is not { } provider)
		{
			return Task.FromResult(default(IStorageFolder));
		}

		return provider.TryGetFolderFromPathAsync(folderPath);
	}
	#endregion
}
