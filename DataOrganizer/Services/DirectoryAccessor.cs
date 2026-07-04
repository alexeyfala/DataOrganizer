using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Explorer;
using Serilog;
using Shared.Common;
using Shared.Enums;
using Shared.Extensions;
using System;
using System.Diagnostics;

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
					Process.Start(AppUtils.PlatformSpecificExplorer, "/select, " + Environment.ProcessPath);
					break;

				case OperatingSystemType.Linux:
					OpenDirectory(Environment.CurrentDirectory);
					break;

				case OperatingSystemType.MacOs:
					Process.Start(AppUtils.PlatformSpecificExplorer, GetMacOsReveal(AppDomain.CurrentDomain.BaseDirectory));
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
