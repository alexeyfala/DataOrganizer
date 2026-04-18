using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using Shared.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class FileSystemPicker : IFileSystemPicker
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;
	#endregion

	#region Constructors
	public FileSystemPicker(Application app) => _app = app;
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<string?> SaveFileAsync<T>(FilePickerSaveOptions options) where T : Window
	{
		if (FindStorageProvider<T>() is not { } provider || await provider
			.SaveFilePickerAsync(options)
			.ConfigureAwait(false) is not { } file)
		{
			return null;
		}

		try
		{
			return file
				.Path
				.AbsolutePath;
		}
		finally
		{
			file.Dispose();
		}
	}

	/// <inheritdoc />
	public async Task<string[]> SelectFilesAsync<T>(FilePickerOpenOptions options) where T : Window
	{
		if (FindStorageProvider<T>() is not { } provider)
		{
			return [];
		}

		IReadOnlyList<IStorageFile> files = await provider
			.OpenFilePickerAsync(options)
			.ConfigureAwait(false);

		try
		{
			return [.. files.Select(x => x.Path.AbsolutePath)];
		}
		finally
		{
			files.ForEach(x => x.Dispose());
		}
	}
	#endregion

	#region Service
	/// <summary>
	/// Tries to get <see cref="IStorageProvider" /> in the application.
	/// </summary>
	private IStorageProvider? FindStorageProvider<T>() where T : Window
	{
		return _app
			.FindWindow<T>()?
			.StorageProvider;
	}
	#endregion
}
