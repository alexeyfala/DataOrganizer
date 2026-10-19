using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions.Editor;
using DataOrganizer.Extensions.Hierarchy;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Runtime;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Encryption;

/// <inheritdoc cref="IProtectionScenarios" />
public sealed class ProtectionScenarios : IProtectionScenarios
{
	#region Data
	/// <inheritdoc cref="IBackgroundServiceControl" />
	private readonly IBackgroundServiceControl _backgroundServiceControl;

	/// <inheritdoc cref="IContentVisibility" />
	private readonly IContentVisibility _contentVisibility;

	/// <inheritdoc cref="IFolderProtection" />
	private readonly IFolderProtection _folderProtection;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <inheritdoc cref="IOpenFilesCloser" />
	private readonly IOpenFilesCloser _openFilesCloser;

	/// <inheritdoc cref="ISessionEditorTabs" />
	private readonly ISessionEditorTabs _sessionEditorTabs;

	/// <inheritdoc cref="ISessionHierarchy" />
	private readonly ISessionHierarchy _sessionHierarchy;
	#endregion

	#region Constructors
	public ProtectionScenarios(
		IBackgroundServiceControl backgroundServiceControl,
		IContentVisibility contentVisibility,
		IFolderProtection folderProtection,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		IOpenFilesCloser openFilesCloser,
		ISessionEditorTabs sessionEditorTabs,
		ISessionHierarchy sessionHierarchy)
	{
		_backgroundServiceControl = backgroundServiceControl;

		_contentVisibility = contentVisibility;

		_folderProtection = folderProtection;

		_logger = logger;

		_messenger = messenger;

		_notification = notification;

		_openFilesCloser = openFilesCloser;

		_sessionEditorTabs = sessionEditorTabs;

		_sessionHierarchy = sessionHierarchy;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task ChangePasswordAsync(FolderDto folder, CancellationToken token = default)
	{
		if (!await _openFilesCloser
			.TryCloseAsync([.. folder.Children.GetFilesBy(static x => x.IsOpened())], token)
			.ConfigureAwait(true))
		{
			return;
		}

		_logger.LogInformation("Change password of the folder");

		await _folderProtection
			.ChangePasswordAsync(folder, token)
			.ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task DecryptFolderAsync(FolderDto folder, CancellationToken token = default)
	{
		if (await TryCloseFilesOfAsync(folder, token).ConfigureAwait(true) is not { } files)
		{
			return;
		}

		_logger.LogInformation("Decrypt files in a folder");

		await _folderProtection
			.DecryptFolderAsync(folder, files, token)
			.ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task EncryptFolderAsync(FolderDto folder, CancellationToken token = default)
	{
		if (await TryCloseFilesOfAsync(folder, token).ConfigureAwait(true) is not { } files)
		{
			return;
		}

		_logger.LogInformation("Encrypt files in a folder");

		await _folderProtection
			.EncryptFolderAsync(folder, files, token)
			.ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task<bool> HideAllContentsAsync(CancellationToken token = default)
	{
		if (!await TryFlushEditorsAsync(token).ConfigureAwait(true))
		{
			return false;
		}

		FileDto[] openedFiles = [.. _sessionHierarchy
			.Hierarchy
			.GetFilesBy(static x => x.IsOpened() && x.EncryptionStatus == EncryptionStatus.Decrypted)];

		if (!await _openFilesCloser
			.TryCloseAsync(openedFiles, token)
			.ConfigureAwait(true))
		{
			return false;
		}

		_contentVisibility.HideAllContents(_sessionHierarchy.Hierarchy);

		_backgroundServiceControl.RefreshAutoLock();

		return true;
	}

	/// <inheritdoc />
	public async Task<bool> HideFileContentsAsync(FileDto file, CancellationToken token = default)
	{
		if (!await TryFlushEditorsAsync(token).ConfigureAwait(true))
		{
			return false;
		}

		if (!await _openFilesCloser
			.TryCloseAsync(file.IsOpened() ? [file] : [], token)
			.ConfigureAwait(true))
		{
			return false;
		}

		_logger.LogInformation("Hide file contents");

		_contentVisibility.HideFileContents(file);

		_backgroundServiceControl.RefreshAutoLock();

		return true;
	}

	/// <inheritdoc />
	public async Task<bool> HideFolderContentsAsync(FolderDto folder, CancellationToken token = default)
	{
		if (!await TryFlushEditorsAsync(token).ConfigureAwait(true))
		{
			return false;
		}

		if (!await _openFilesCloser
			.TryCloseAsync([.. folder.Children.GetFilesBy(static x => x.IsOpened())], token)
			.ConfigureAwait(true))
		{
			return false;
		}

		_logger.LogInformation("Hide files in a folder");

		_contentVisibility.HideFolderContents(folder);

		_backgroundServiceControl.RefreshAutoLock();

		return true;
	}

	/// <inheritdoc />
	public async Task LockAsync(CancellationToken token = default)
	{
		_logger.LogInformation("Auto-lock: hiding the decrypted contents");

		// Unlike hiding by hand, the lock is not called off by an editor that failed to persist its changes.
		await _messenger
			.FlushEditorsAsync(token)
			.ConfigureAwait(true);

		_openFilesCloser.Close([.. _sessionHierarchy
			.Hierarchy
			.GetFilesBy(static x => x.IsOpened())]);

		// No editor shows the tabs that wait in the work session, so their files are unmarked here.
		_sessionEditorTabs
			.EditorTabs?
			.Files
			.ForEach(static x => x.IsEditing = false);

		_sessionEditorTabs.EditorTabs = null;

		_contentVisibility.HideAllContents(_sessionHierarchy.Hierarchy);

		_backgroundServiceControl.RefreshAutoLock();

		_notification.ShowInformationSnackbar(Strings.ContentsHiddenByAutoLock);
	}

	/// <inheritdoc />
	public async Task<bool> ShowFileContentsAsync(FileDto file, CancellationToken token = default)
	{
		_logger.LogInformation("Show file contents");

		if (!await _contentVisibility
			.ShowFileContentsAsync(file, token)
			.ConfigureAwait(true))
		{
			return false;
		}

		_backgroundServiceControl.RefreshAutoLock();

		return true;
	}

	/// <inheritdoc />
	public async Task<bool> ShowFolderContentsAsync(FolderDto folder, CancellationToken token = default)
	{
		if (await TryCloseFilesOfAsync(folder, token).ConfigureAwait(true) is null)
		{
			return false;
		}

		_logger.LogInformation("Show file contents in a folder");

		await _contentVisibility
			.ShowFolderContentsAsync(folder, token)
			.ConfigureAwait(true);

		_backgroundServiceControl.RefreshAutoLock();

		return true;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the files of a folder once their open ones are closed; <c>null</c> when the folder has no files, which the
	/// user is told, or when the user keeps the files open.
	/// </summary>
	private async Task<FileDto[]?> TryCloseFilesOfAsync(FolderDto folder, CancellationToken token)
	{
		FileDto[] files = [.. folder
			.Children
			.GetFiles()];

		if (files.IsEmpty())
		{
			_notification.ShowInformationSnackbar(Strings.MissingFiles);

			return null;
		}

		if (!await _openFilesCloser
			.TryCloseAsync([.. files.Where(static x => x.IsOpened())], token)
			.ConfigureAwait(true))
		{
			return null;
		}

		return files;
	}

	/// <summary>
	/// Persists the pending changes of the open editors before the key is dropped; <c>False</c> reports the failure to
	/// the user and keeps the caller from hiding anything.
	/// </summary>
	private async Task<bool> TryFlushEditorsAsync(CancellationToken token)
	{
		if (await _messenger
			.FlushEditorsAsync(token)
			.ConfigureAwait(true))
		{
			return true;
		}

		_logger.LogWarning("Contents are not hidden: an editor failed to persist its changes");

		_notification.ShowErrorSnackbar(Strings.FailedToProcessContents);

		return false;
	}
	#endregion
}
