using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Threading;

namespace Repository.Services;

/// <summary>
/// A copy of the database that is erased when the operation owning it ends.
/// </summary>
public sealed class DatabaseBackup : IDisposable
{
	#region Data
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
}
