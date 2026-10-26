using AppLifecycle.Interfaces;
using GlobalHotkeys.Extensions;
using GlobalHotkeys.Interfaces;
using Hierarchy.Dto;
using HierarchyStorage.Interfaces;
using Notifications.Interfaces;
using Repository.Enums;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System.Threading;
using System.Threading.Tasks;
using WindowContracts.Interfaces;

namespace AppLifecycle.Services;

/// <inheritdoc cref="IStartupDataLoader" />
public sealed class StartupDataLoader : IStartupDataLoader
{
	#region Data
	/// <inheritdoc cref="IDbConnector" />
	private readonly IDbConnector _dbConnector;

	/// <inheritdoc cref="IEntityLoader" />
	private readonly IEntityLoader _entityLoader;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INoticeLauncher" />
	private readonly INoticeLauncher _noticeLauncher;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <inheritdoc cref="IUnreadableHotkeysFinder" />
	private readonly IUnreadableHotkeysFinder _unreadableHotkeys;
	#endregion

	#region Constructors
	public StartupDataLoader(
		IDbConnector dbConnector,
		IEntityLoader entityLoader,
		ILogger logger,
		INoticeLauncher noticeLauncher,
		INotificationService notification,
		IUnreadableHotkeysFinder unreadableHotkeys)
	{
		_dbConnector = dbConnector;

		_entityLoader = entityLoader;

		_logger = logger;

		_noticeLauncher = noticeLauncher;

		_notification = notification;

		_unreadableHotkeys = unreadableHotkeys;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<LoadedHierarchy?> LoadAsync(CancellationToken token = default)
	{
		DbConnectionStatus status = await _dbConnector
			.ConnectAsync(token)
			.ConfigureAwait(true);

		if (status is DbConnectionStatus.SchemaTooOld or DbConnectionStatus.SchemaTooNew)
		{
			// The data is intact, and an empty hierarchy would read as a loss worth undoing.
			_logger.LogError($"The schema of the database is {status}, the launch ends.", breakInDebugger: false);

			await _noticeLauncher
				.ShowStartupErrorAsync(_dbConnector.GetDbFilePath())
				.ConfigureAwait(true);

			return null;
		}

		if (status is not DbConnectionStatus.Connected)
		{
			_logger.LogError("The database is unavailable, the launch continues without it.", breakInDebugger: false);

			_notification.ShowToast(Strings.DatabaseIsUnavailable);

			// Nothing is read from a database that is not there: the toast above has already said so.
			return LoadedHierarchy.Empty;
		}

		if (await _entityLoader
			.LoadHierarchyAsync(token)
			.ConfigureAwait(true) is not { } loaded)
		{
			_logger.LogError(
				"The database could not be read, the launch continues with an empty hierarchy.",
				breakInDebugger: false);

			_notification.ShowToast(Strings.FailedToReadDatabase);

			return LoadedHierarchy.Empty;
		}

		FileDto[] unreadable = _unreadableHotkeys.Find(loaded.Items);

		if (unreadable.IsNotEmpty())
		{
			_notification.ShowToast(unreadable.GetUnreadableHotkeysPresentation(Strings.FailedToReadHotkeys));
		}

		return loaded;
	}
	#endregion
}
