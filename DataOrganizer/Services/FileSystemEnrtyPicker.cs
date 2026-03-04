using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class FileSystemEnrtyPicker : IFileSystemEnrtyPicker
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;
	#endregion

	#region Constructors
	public FileSystemEnrtyPicker(Application app) => _app = app;
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<string?> SaveFileAsync<T>(FilePickerSaveOptions options) where T : Window
	{
		if (_app.FindWindow<T>()?.StorageProvider is not { } storageProvider || await storageProvider
			.SaveFilePickerAsync(options)
			.ConfigureAwait(false) is not { } file)
		{
			return null;
		}

		return file
			.Path
			.AbsolutePath;
	}
	#endregion
}
