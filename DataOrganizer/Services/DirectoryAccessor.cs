using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Explorer;
using Serilog;
using Shared.Common;
using Shared.Enums;
using Shared.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DataOrganizer.Services;

public sealed class DirectoryAccessor : IDirectoryAccessor
{
	#region Data
	/// <inheritdoc cref="ILinuxExplorerManager" />
	private readonly ILinuxExplorerManager _linuxExplorerManager;

	/// <inheritdoc cref="IWindowsExplorerManager" />
	private readonly IWindowsExplorerManager _winExplorerManager;
	#endregion

	#region Constructors
	public DirectoryAccessor(
		ILinuxExplorerManager linuxExplorerManager,
		IWindowsExplorerManager winExplorerManager)
	{
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
			switch (AppUtils.CurrentOs)
			{
				case OperatingSystemType.Windows:
					string appPath = Environment.ProcessPath!;

					string appDirectory = Path.GetDirectoryName(appPath)!;

					if (_winExplorerManager.TryForegroundFolder(appDirectory, appPath))
					{
						return;
					}

					Process.Start(AppUtils.PlatformSpecificExplorer, "/select, " + appPath);
					break;

				case OperatingSystemType.Linux:
					string? linuxAppFile = ResolveLinuxAppFile();

					string linuxAppDirectory = linuxAppFile is not null
						? Path.GetDirectoryName(linuxAppFile)!
						: AppContext.BaseDirectory;

					// Reuse an already-open window if possible (X11 cannot select the file inside it).
					if (_linuxExplorerManager.TryForegroundFolder(linuxAppDirectory))
					{
						return;
					}

					// Otherwise open a fresh window with the file selected.
					if (linuxAppFile is not null && _linuxExplorerManager.TryRevealFile(linuxAppFile))
					{
						return;
					}

					OpenDirectory(AppContext.BaseDirectory);
					break;

				case OperatingSystemType.MacOs:
					string macOsRevealTarget = ResolveMacOsBundle() ?? AppContext.BaseDirectory;

					Process.Start(AppUtils.PlatformSpecificExplorer, GetMacOsReveal(macOsRevealTarget));
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
	#endregion

	#region Helpers
	/// <summary>
	/// Combines the path with the folder expansion argument for <see cref="OperatingSystemType.MacOs" />.
	/// </summary>
	private static string GetMacOsReveal(string argument) => $@"-R ""{argument}""";

	/// <summary>
	/// Resolves the application file to reveal on <see cref="OperatingSystemType.Linux" /> — the native
	/// apphost next to the app, falling back to the entry assembly; <c>null</c> when neither exists.
	/// </summary>
	/// <remarks>
	/// <see cref="Environment.ProcessPath" /> is unreliable here: launching through the shared
	/// <c>dotnet</c> host points it at the muxer instead of the application.
	/// </remarks>
	private static string? ResolveLinuxAppFile()
	{
		Assembly? entryAssembly = Assembly.GetEntryAssembly();

		string? appName = entryAssembly?
			.GetName()
			.Name;

		if (!string.IsNullOrEmpty(appName))
		{
			string appHost = Path.Combine(AppContext.BaseDirectory, appName);

			if (File.Exists(appHost))
			{
				return appHost;
			}
		}

		string? entryLocation = entryAssembly?.Location;

		if (!string.IsNullOrEmpty(entryLocation) && File.Exists(entryLocation))
		{
			return entryLocation;
		}

		return null;
	}

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
	#endregion
}
