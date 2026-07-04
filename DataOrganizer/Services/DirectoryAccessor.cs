using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Explorer;
using Serilog;
using Shared.Common;
using Shared.Enums;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DataOrganizer.Services;

public sealed class DirectoryAccessor : IDirectoryAccessor
{
	#region Data
	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ILinuxExplorerManager" />
	private readonly ILinuxExplorerManager _linuxExplorerManager;

	/// <inheritdoc cref="IWindowsExplorerManager" />
	private readonly IWindowsExplorerManager _winExplorerManager;
	#endregion

	#region Constructors
	public DirectoryAccessor(
		IFileSystem fileSystem,
		ILinuxExplorerManager linuxExplorerManager,
		IWindowsExplorerManager winExplorerManager)
	{
		_fileSystem = fileSystem;

		_linuxExplorerManager = linuxExplorerManager;

		_winExplorerManager = winExplorerManager;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void OpenAppDirectory(ILogger? logger = null)
	{
		try
		{
			string? appTarget = AppUtils.CurrentOs switch
			{
				OperatingSystemType.Windows => Environment.ProcessPath,
				OperatingSystemType.Linux => ResolveLinuxAppFile(),
				OperatingSystemType.MacOs => ResolveMacOsBundle(),
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
			if (AppUtils.IsWindows)
			{
				try
				{
					if (_winExplorerManager.TryForegroundFolder(directoryPath))
					{
						return;
					}
				}
				catch (Exception ex)
				{
					logger?.LogException(ex);
				}
			}
			else if (AppUtils.IsLinux)
			{
				try
				{
					if (_linuxExplorerManager.TryForegroundFolder(directoryPath))
					{
						return;
					}
				}
				catch (Exception ex)
				{
					logger?.LogException(ex);
				}
			}

			Process.Start(
				AppUtils.PlatformSpecificExplorer,
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
				|| (!_fileSystem.IsFileExists(filePath) && !_fileSystem.IsDirectoryExists(filePath)))
			{
				if (Path.GetDirectoryName(filePath) is { Length: > 0 } fallbackDirectory)
				{
					OpenDirectory(fallbackDirectory, logger);
				}

				return;
			}

			string directory = Path.GetDirectoryName(filePath)!;

			switch (AppUtils.CurrentOs)
			{
				case OperatingSystemType.Windows:
					if (_winExplorerManager.TryForegroundFolder(directory, filePath))
					{
						return;
					}

					Process.Start(AppUtils.PlatformSpecificExplorer, "/select, " + filePath);
					break;

				case OperatingSystemType.Linux:
					// Reuse an already-open window if possible (X11 cannot select the file inside it).
					if (_linuxExplorerManager.TryForegroundFolder(directory))
					{
						return;
					}

					// Otherwise open a fresh window with the file selected.
					if (_linuxExplorerManager.TryRevealFile(filePath))
					{
						return;
					}

					OpenDirectory(directory, logger);
					break;

				case OperatingSystemType.MacOs:
					Process.Start(AppUtils.PlatformSpecificExplorer, GetMacOsReveal(filePath));
					break;

				default:
					throw new NotImplementedException();
			}
		}
		catch (Exception ex)
		{
			logger?.LogException(ex);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Combines the path with the folder expansion argument for <see cref="OperatingSystemType.MacOs" />.
	/// </summary>
	private static string GetMacOsReveal(string argument) => $@"-R ""{argument}""";

	/// <summary>
	/// Resolves the enclosing <c>.app</c> bundle on <see cref="OperatingSystemType.MacOs" /> by walking up
	/// from <see cref="AppContext.BaseDirectory" />; <c>null</c> when the app runs outside a bundle.
	/// </summary>
	private static string? ResolveMacOsBundle()
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
	/// Resolves the application file to reveal on <see cref="OperatingSystemType.Linux" /> — the native
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

			if (_fileSystem.IsFileExists(appHost))
			{
				return appHost;
			}
		}

		string? entryLocation = entryAssembly?.Location;

		if (!string.IsNullOrEmpty(entryLocation) && _fileSystem.IsFileExists(entryLocation))
		{
			return entryLocation;
		}

		return null;
	}
	#endregion
}
