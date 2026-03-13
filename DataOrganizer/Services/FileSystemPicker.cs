using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class FileSystemPicker : IFileSystemPicker
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IDispatcher" />
	private readonly IDispatcher _dispatcher;
	#endregion

	#region Constructors
	public FileSystemPicker(Application app, IDispatcher dispatcher)
	{
		_app = app;

		_dispatcher = dispatcher;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<string?> SaveFileAsync<T>(FilePickerSaveOptions options) where T : Window
	{
		if (await FindStorageProviderAsync<T>().ConfigureAwait(false) is not { } provider || await provider
			.SaveFilePickerAsync(options)
			.ConfigureAwait(false) is not { } file)
		{
			return null;
		}

		return file
			.Path
			.AbsolutePath;
	}

	/// <inheritdoc />
	public async Task<string[]> SelectFilesAsync<T>(FilePickerOpenOptions options) where T : Window
	{
		if (await FindStorageProviderAsync<T>().ConfigureAwait(false) is not { } provider)
		{
			return [];
		}

		IReadOnlyList<IStorageFile> files = await provider
			.OpenFilePickerAsync(options)
			.ConfigureAwait(false);

		return [.. files.Select(x => x.Path.AbsolutePath)];
	}
	#endregion

	#region Service
	/// <summary>
	/// Tries to get <see cref="IStorageProvider" /> in the application.
	/// </summary>
	private Task<IStorageProvider?> FindStorageProviderAsync<T>() where T : Window
	{
		TaskCompletionSource<IStorageProvider?> source = new();

		_dispatcher.Post(() =>
		{
			source.TrySetResult(_app
				.FindWindow<T>()?
				.StorageProvider);
		});

		return source.Task;
	}
	#endregion
}
