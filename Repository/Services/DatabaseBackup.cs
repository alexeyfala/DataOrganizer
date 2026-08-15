using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.IO;
using System.Threading;

namespace Repository.Services;

/// <summary>
/// A copy of the database that is erased when the operation owning it ends.
/// </summary>
public sealed class DatabaseBackup : IDisposable
{
	#region Data
	/// <summary>
	/// Folder the copies live in, next to the database.
	/// </summary>
	private const string DirectoryName = "Backups";

	/// <summary>
	/// Name a copy had before the copies moved into a folder of their own.
	/// </summary>
	private const string LegacyFileName = "Backup" + AppUtils.SQLiteExtension;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <summary>
	/// <c>True</c> when the copy has already been erased.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	public DatabaseBackup(
		string filePath,
		IFileSystem fileSystem,
		ILogger logger)
	{
		FilePath = filePath;

		_fileSystem = fileSystem;

		_logger = logger;
	}
	#endregion

	#region Properties
	/// <summary>
	/// Path of the copy.
	/// </summary>
	public string FilePath { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Builds the path of a new copy of the given database.
	/// </summary>
	public static string CreateFilePath(string databaseFilePath)
	{
		return Path.Combine(
			GetDirectoryPath(databaseFilePath),
			Guid.NewGuid().ToString("N") + AppUtils.SQLiteExtension);
	}

	/// <summary>
	/// Returns the folder holding the copies of the given database.
	/// </summary>
	public static string GetDirectoryPath(string databaseFilePath)
	{
		return Path.Combine(GetDatabaseDirectoryPath(databaseFilePath), DirectoryName);
	}

	/// <summary>
	/// Returns the path a copy of the given database had in the previous versions.
	/// </summary>
	public static string GetLegacyFilePath(string databaseFilePath)
	{
		return Path.Combine(GetDatabaseDirectoryPath(databaseFilePath), LegacyFileName);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

		try
		{
			if (_fileSystem.IsFileExists(FilePath))
			{
				_fileSystem.EraseAndDeleteFile(FilePath);
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the folder the database itself lies in.
	/// </summary>
	private static string GetDatabaseDirectoryPath(string databaseFilePath)
	{
		return Path.GetDirectoryName(databaseFilePath) ?? string.Empty;
	}
	#endregion
}
