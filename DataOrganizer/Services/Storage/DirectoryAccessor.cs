using DataOrganizer.Interfaces.Explorer;
using DataOrganizer.Interfaces.Storage;
using Serilog;
using Shared.Common;
using Shared.Enums;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DataOrganizer.Services.Storage;

public sealed class DirectoryAccessor : IDirectoryAccessor
{
	#region Data
	/// <inheritdoc cref="IExplorerManager" />
	private readonly IExplorerManager _explorerManager;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;
	#endregion

	#region Constructors
	public DirectoryAccessor(IExplorerManager explorerManager, IFileSystem fileSystem)
	{
		_explorerManager = explorerManager;

		_fileSystem = fileSystem;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void OpenAppDirectory(ILogger? logger = null)
	{
		try
		{
			string? appTarget = PlatformInfo.CurrentOS switch
			{
				OperatingSystemKind.Windows => Environment.ProcessPath,
				OperatingSystemKind.Linux => ResolveLinuxAppFile(),
				OperatingSystemKind.MacOS => ResolveMacOSBundle(),
				_ => throw new NotImplementedException()
			};

			if (!string.IsNullOrEmpty(appTarget))
			{
				RevealFile(appTarget, logger);
			}
			else
			{
				OpenDirectory(AppContext.BaseDirectory, logger);
			}
		}
		catch (Exception ex)
		{
			logger?.LogException(ex);
		}
	}

	/// <inheritdoc />
	public void OpenDirectory(string directoryPath, ILogger? logger = null)
	{
		try
		{
			try
			{
				if (_explorerManager.TryForegroundFolder(directoryPath))
				{
					return;
				}
			}
			catch (Exception ex)
			{
				logger?.LogException(ex);
			}

			Process.Start(
				PlatformInfo.FileOpener,
				directoryPath.SurroundWithQuotesIfNeeded());
		}
		catch (Exception ex)
		{
			logger?.LogException(ex);
		}
	}

	/// <inheritdoc />
	public void RevealFile(string filePath, ILogger? logger = null)
	{
		try
		{
			// The target may be a file or, on macOS, an .app bundle (a directory) — accept both.
			if (string.IsNullOrEmpty(filePath)
				|| (!_fileSystem.FileExists(filePath) && !_fileSystem.DirectoryExists(filePath)))
			{
				if (Path.GetDirectoryName(filePath) is { Length: > 0 } fallbackDirectory)
				{
					OpenDirectory(fallbackDirectory, logger);
				}

				return;
			}

			string directory = Path.GetDirectoryName(filePath)!;

			// Reuse an already-open window if possible.
			if (_explorerManager.TryForegroundFolder(directory, filePath))
			{
				return;
			}

			// Otherwise open a fresh window with the file selected.
			if (_explorerManager.TryRevealFile(filePath))
			{
				return;
			}

			OpenDirectory(directory, logger);
		}
		catch (Exception ex)
		{
			logger?.LogException(ex);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Resolves the enclosing <c>.app</c> bundle on <see cref="OperatingSystemKind.MacOS" /> by walking up
	/// from <see cref="AppContext.BaseDirectory" />; <c>null</c> when the app runs outside a bundle.
	/// </summary>
	private static string? ResolveMacOSBundle()
	{
		const string bundleExtension = ".app";

		DirectoryInfo? directory = new(AppContext.BaseDirectory);

		while (directory is not null)
		{
			if (directory.Name.EndsWith(bundleExtension, StringComparison.OrdinalIgnoreCase))
			{
				return directory.FullName;
			}

			directory = directory.Parent;
		}

		return null;
	}

	/// <summary>
	/// Resolves the application file to reveal on <see cref="OperatingSystemKind.Linux" /> — the native
	/// apphost next to the app, falling back to the entry assembly; <c>null</c> when neither exists.
	/// </summary>
	/// <remarks>
	/// <see cref="Environment.ProcessPath" /> is unreliable here: launching through the shared
	/// <c>dotnet</c> host points it at the muxer instead of the application.
	/// </remarks>
	private string? ResolveLinuxAppFile()
	{
		Assembly? entryAssembly = Assembly.GetEntryAssembly();

		string? appName = entryAssembly?
			.GetName()
			.Name;

		if (!string.IsNullOrEmpty(appName))
		{
			string appHost = Path.Combine(AppContext.BaseDirectory, appName);

			if (_fileSystem.FileExists(appHost))
			{
				return appHost;
			}
		}

		string? entryLocation = entryAssembly?.Location;

		if (!string.IsNullOrEmpty(entryLocation) && _fileSystem.FileExists(entryLocation))
		{
			return entryLocation;
		}

		return null;
	}
	#endregion
}
