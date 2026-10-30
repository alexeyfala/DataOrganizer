using HierarchyExchange.Interfaces;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Common;
using System.Threading;
using System.Threading.Tasks;

namespace HierarchyExchange.Services;

/// <summary>
/// The SQLite format of the exchange files: a file is a copy of the database.
/// </summary>
public sealed class SqliteExchangeFormat : IExchangeFormat
{
	#region Data
	/// <inheritdoc cref="IDbBackupService" />
	private readonly IDbBackupService _dbBackupService;

	/// <inheritdoc cref="IDbImporter" />
	private readonly IDbImporter _dbImporter;
	#endregion

	#region Constructors
	public SqliteExchangeFormat(IDbBackupService dbBackupService, IDbImporter dbImporter)
	{
		_dbBackupService = dbBackupService;

		_dbImporter = dbImporter;
	}
	#endregion

	#region Properties
	/// <inheritdoc />
	public string Extension => KnownFileExtensions.Sqlite;

	/// <inheritdoc />
	public string FileTypeName => "SQLite Database File";

	/// <inheritdoc />
	public bool IsDatabaseCopy => true;

	/// <inheritdoc />
	public string MimeType => "application/x-sqlite3";
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task ExportAsync(string filePath, CancellationToken token = default)
	{
		return _dbBackupService.CopyToAsync(filePath, token);
	}

	/// <inheritdoc />
	public Task<LoadedEntities?> ReadAsync(string filePath, CancellationToken token = default)
	{
		LoadedEntities? result = _dbImporter.IsValidSqliteDatabase(filePath)
			? _dbImporter.LoadEntities(filePath)
			: null;

		return Task.FromResult(result);
	}
	#endregion
}
