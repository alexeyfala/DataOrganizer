using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Explorer;
using Serilog;
using Shared.Common;
using Shared.Enums;
using Shared.Extensions;
using System;
using System.Diagnostics;
using System.IO;

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
					if (_linuxExplorerManager.TryRevealFile(Environment.ProcessPath!))
					{
						return;
					}

					OpenDirectory(AppContext.BaseDirectory);
					break;

				case OperatingSystemType.MacOs:
					Process.Start(AppUtils.PlatformSpecificExplorer, GetMacOsReveal(AppContext.BaseDirectory));
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
	#endregion
}
