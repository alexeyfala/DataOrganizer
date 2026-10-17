using Avalonia.Platform.Storage;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Views;
using Shared.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Dialogs;

public sealed class FileSystemPicker : IFileSystemPicker
{
	#region Data
	/// <inheritdoc cref="IWindowFinder" />
	private readonly IWindowFinder _windowFinder;
	#endregion

	#region Constructors
	public FileSystemPicker(IWindowFinder windowFinder) => _windowFinder = windowFinder;
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<string?> SaveFileAsync(FileSaveParameters parameters)
	{
		if (FindStorageProvider() is not { } provider || await provider
			.SaveFilePickerAsync(FilePickerOptionsMapper.ToSaveOptions(parameters))
			.ConfigureAwait(false) is not { } file)
		{
			return null;
		}

		try
		{
			return file
				.Path
				.LocalPath;
		}
		finally
		{
			file.Dispose();
		}
	}

	/// <inheritdoc />
	public async Task<string[]> SelectFilesAsync(FileOpenParameters parameters)
	{
		if (FindStorageProvider() is not { } provider)
		{
			return [];
		}

		IReadOnlyList<IStorageFile> files = await provider
			.OpenFilePickerAsync(FilePickerOptionsMapper.ToOpenOptions(parameters))
			.ConfigureAwait(false);

		try
		{
			return [.. files.Select(x => x.Path.LocalPath)];
		}
		finally
		{
			files.ForEach(x => x.Dispose());
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Tries to get <see cref="IStorageProvider" /> of the main window.
	/// </summary>
	private IStorageProvider? FindStorageProvider()
	{
		return _windowFinder
			.FindMainWindow()?
			.StorageProvider;
	}
	#endregion
}
