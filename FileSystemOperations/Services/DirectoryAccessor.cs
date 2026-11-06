using FileSystemOperations.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Enums;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;

namespace FileSystemOperations.Services;

/// <inheritdoc cref="IDirectoryAccessor" />
public sealed class DirectoryAccessor : IDirectoryAccessor
{
	#region Data
	/// <summary>
	/// Environment variable an AppImage sets to its own path.
	/// </summary>
	private const string AppImageVariable = "APPIMAGE";

	/// <inheritdoc cref="IAppProcess" />
	private readonly IAppProcess _appProcess;

	/// <inheritdoc cref="IExplorerManager" />
	private readonly IExplorerManager _explorerManager;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IPlatform" />
	private readonly IPlatform _platform;
	#endregion

	#region Constructors
	public DirectoryAccessor(
		IAppProcess appProcess,
		IExplorerManager explorerManager,
		IFileSystem fileSystem,
		ILogger logger,
		IPlatform platform)
	{
		_appProcess = appProcess;

		_explorerManager = explorerManager;

		_fileSystem = fileSystem;

		_logger = logger;

		_platform = platform;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void OpenAppDirectory()
	{
		try
		{
			string? appTarget = _platform.CurrentOS switch
			{
				OperatingSystemKind.Windows or OperatingSystemKind.Linux => ResolveAppFile(),
				OperatingSystemKind.MacOS => ResolveMacOSBundle(_appProcess.BaseDirectory),
				_ => throw new NotImplementedException()
			};

			if (!string.IsNullOrEmpty(appTarget))
			{
				RevealFile(appTarget);
			}
			else
			{
				OpenDirectory(_appProcess.BaseDirectory);
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <inheritdoc />
	[ExcludeFromCodeCoverage(Justification = "Brings forward or starts the file manager of the system")]
	public void OpenDirectory(string directoryPath)
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
				_logger.LogException(ex);
			}

			Process.Start(
				PlatformInfo.FileOpener,
				directoryPath.SurroundWithQuotesIfNeeded());
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <inheritdoc />
	public void RevealFile(string filePath)
	{
		try
		{
			// The target may be a file or, on macOS, an .app bundle (a directory) — accept both.
			if (string.IsNullOrEmpty(filePath)
				|| (!_fileSystem.FileExists(filePath) && !_fileSystem.DirectoryExists(filePath)))
			{
				if (Path.GetDirectoryName(filePath) is { Length: > 0 } fallbackDirectory)
				{
					OpenDirectory(fallbackDirectory);
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

			OpenDirectory(directory);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Resolves the enclosing <c>.app</c> bundle on <see cref="OperatingSystemKind.MacOS" /> by walking up
	/// from <paramref name="baseDirectory" />; <c>null</c> when the app runs outside a bundle.
	/// </summary>
	[ExcludeFromCodeCoverage(Justification = "Runs on macOS only, while the mutation run is on Windows")]
	private static string? ResolveMacOSBundle(string baseDirectory)
	{
		const string bundleExtension = ".app";

		DirectoryInfo? directory = new(baseDirectory);

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
	/// Resolves the application file to reveal on <see cref="OperatingSystemKind.Windows" /> and
	/// <see cref="OperatingSystemKind.Linux" />; <c>null</c> when none exists.
	/// </summary>
	private string? ResolveAppFile()
	{
		// An AppImage runs from a temporary mount, so the file to show is the AppImage itself.
		if (_platform.IsLinux
			&& _appProcess.GetEnvironmentVariable(AppImageVariable) is { Length: > 0 } appImagePath
			&& _fileSystem.FileExists(appImagePath))
		{
			return appImagePath;
		}

		// The shared dotnet host runs an application from its entry assembly and is not a file of the application.
		if (_appProcess.ProcessPath is { Length: > 0 } processPath
			&& !_appProcess.IsRunByDotnetHost()
			&& _fileSystem.FileExists(processPath))
		{
			return processPath;
		}

		if (_appProcess.EntryAssemblyName is not { Length: > 0 } appName)
		{
			return null;
		}

		string appHostPath = Path.Combine(
			_appProcess.BaseDirectory,
			appName + (_platform.IsWindows ? KnownFileExtensions.Exe : string.Empty));

		if (_fileSystem.FileExists(appHostPath))
		{
			return appHostPath;
		}

		return _appProcess.GetEntryAssemblyPath() is { } entryAssemblyPath && _fileSystem.FileExists(entryAssemblyPath)
			? entryAssemblyPath
			: null;
	}
	#endregion
}
