using DataOrganizer.Database.Repository.Interfaces.Database;
using DataOrganizer.Database.Repository.Services.Database;
using DataOrganizer.Infrastructure.Diagnostics.Interfaces;
using DataOrganizer.Infrastructure.FileSystemOperations.Interfaces;
using DataOrganizer.Infrastructure.Notifications.Interfaces;
using DataOrganizer.Shared.Extensions;
using DataOrganizer.Shared.Properties;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Infrastructure.FileSystemOperations.Services;

/// <inheritdoc cref="IBackedUpWriter" />
public sealed class BackedUpWriter : IBackedUpWriter
{
	#region Data
	/// <inheritdoc cref="IDbBackupService" />
	private readonly IDbBackupService _dbBackupService;

	/// <inheritdoc cref="IDbFailureReporter" />
	private readonly IDbFailureReporter _dbFailureReporter;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public BackedUpWriter(
		IDbBackupService dbBackupService,
		IDbFailureReporter dbFailureReporter,
		ILogger logger,
		INotificationService notification)
	{
		_dbBackupService = dbBackupService;

		_dbFailureReporter = dbFailureReporter;

		_logger = logger;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<bool> WriteAsync(Func<Task<bool>> write, CancellationToken token = default)
	{
		using DatabaseBackup? backup = await TryCreateBackupAsync(token).ConfigureAwait(false);

		if (backup is null)
		{
			_notification.ShowErrorSnackbar(Strings.UnableToCreateDatabaseBackup);

			return false;
		}

		bool isWritten;

		try
		{
			isWritten = await write().ConfigureAwait(false);
		}
		catch
		{
			await RollBackAsync(backup).ConfigureAwait(false);

			throw;
		}

		if (!isWritten)
		{
			await RollBackAsync(backup).ConfigureAwait(false);
		}

		return isWritten;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Brings the database back to the copy, cancelled or not, as a write stopped halfway has to be undone whole.
	/// </summary>
	private async Task RollBackAsync(DatabaseBackup backup)
	{
		try
		{
			await _dbBackupService
				.RestoreFromBackupAsync(backup.FilePath, CancellationToken.None)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			// A rollback that did not happen leaves the write half-done, which has to be said out loud.
			_dbFailureReporter.Report(ex, Strings.FailedToRestoreDatabase);
		}
	}

	/// <summary>
	/// Makes a copy of the database; <c>null</c> when the copy could not be made.
	/// </summary>
	private async Task<DatabaseBackup?> TryCreateBackupAsync(CancellationToken token)
	{
		try
		{
			return await _dbBackupService
				.CreateBackupAsync(token)
				.ConfigureAwait(false);
		}
		// Stryker disable once Block : the block only logs and returns what an empty block returns
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			return null;
		}
	}
	#endregion
}
