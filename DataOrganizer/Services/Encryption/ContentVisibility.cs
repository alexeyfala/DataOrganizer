using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Notifications;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Encryption;

public sealed class ContentVisibility : IContentVisibility
{
	#region Data
	/// <inheritdoc cref="IEncryptionFailureReporter" />
	private readonly IEncryptionFailureReporter _failureReporter;

	/// <inheritdoc cref="IKeeperUnlocker" />
	private readonly IKeeperUnlocker _keeperUnlocker;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <inheritdoc cref="ISessionKeyStore" />
	private readonly ISessionKeyStore _sessionKeyStore;
	#endregion

	#region Constructors
	public ContentVisibility(
		IEncryptionFailureReporter failureReporter,
		IKeeperUnlocker keeperUnlocker,
		IMessenger messenger,
		INotificationService notification,
		ISessionKeyStore sessionKeyStore)
	{
		_failureReporter = failureReporter;

		_keeperUnlocker = keeperUnlocker;

		_messenger = messenger;

		_notification = notification;

		_sessionKeyStore = sessionKeyStore;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void DiscardAllKeys() => _sessionKeyStore.LockAll();

	/// <inheritdoc />
	public void DiscardKeys(FolderDto folder)
	{
		// A folder that keeps no key is simply not in the store, so being a keeper is not worth a test.
		_sessionKeyStore.Lock(folder.Id);

		folder
			.GetAllChildren()
			.OfType<FolderDto>()
			.ForEach(x => _sessionKeyStore.Lock(x.Id));
	}

	/// <inheritdoc />
	public void HideAllContents(IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		hierarchy
			.FilterBy(x => x.EncryptionStatus == EncryptionStatus.Decrypted)
			.ForEach(x => x.EncryptionStatus = EncryptionStatus.Encrypted);

		_sessionKeyStore.LockAll();
	}

	/// <inheritdoc />
	public void HideFileContents(FileDto file)
	{
		file.EncryptionStatus = EncryptionStatus.Encrypted;

		LockKeeperOf(file);
	}

	/// <inheritdoc />
	public void HideFolderContents(FolderDto folder)
	{
		folder
			.ToEnumerable()
			.Concat(folder.GetAllChildren())
			.ForEach(x => x.EncryptionStatus = EncryptionStatus.Encrypted);

		LockKeeperOf(folder);
	}

	/// <inheritdoc />
	public async Task<bool> ShowFileContentsAsync(FileDto file, CancellationToken token = default)
	{
		if (file.FindPasswordKeeper() is not { } root || root.EncryptedDek is null)
		{
			return false;
		}

		using PinnedBuffer? dek = await _keeperUnlocker.RequestDekAsync(
			keeper: root,
			header: Strings.ShowContents,
			token: token).ConfigureAwait(false);

		if (dek is null)
		{
			return false;
		}

		try
		{
			using ProgressScope _ = _messenger.ShowProgress();

			if (!_sessionKeyStore.Unlock(root.Id, dek))
			{
				_notification.ShowErrorSnackbar(Strings.FailedToShowFileContents);

				return false;
			}

			file.EncryptionStatus = EncryptionStatus.Decrypted;

			return true;
		}
		catch (Exception ex) when (EncryptionFailures.IsCryptographic(ex))
		{
			_failureReporter.Report(ex);

			return false;
		}
	}

	/// <inheritdoc />
	public async Task ShowFolderContentsAsync(FolderDto folder, CancellationToken token = default)
	{
		if (folder.FindPasswordKeeper() is not { } root || root.EncryptedDek is null)
		{
			return;
		}

		using PinnedBuffer? dek = await _keeperUnlocker.RequestDekAsync(
			keeper: root,
			header: Strings.ShowContents,
			token: token).ConfigureAwait(false);

		if (dek is null)
		{
			return;
		}

		try
		{
			using ProgressScope _ = _messenger.ShowProgress();

			if (ShowFolderContents(folder, root.Id, dek))
			{
				return;
			}

			_notification.ShowErrorSnackbar(Strings.FailedToShowFileContents);
		}
		catch (Exception ex) when (EncryptionFailures.IsCryptographic(ex))
		{
			_failureReporter.Report(ex);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Drops the key of the keeper the object belongs to, but only once nothing under that keeper is shown.
	/// </summary>
	private void LockKeeperOf(ExplorerItemDtoBase item)
	{
		FolderDto? keeper = item.FindPasswordKeeper();

		if (keeper?
			.ToEnumerable()
			.Concat(keeper.GetAllChildren())
			.ContainsBy(x => x.EncryptionStatus == EncryptionStatus.Decrypted) != false)
		{
			return;
		}

		_sessionKeyStore.Lock(keeper.Id);
	}

	/// <summary>
	/// Shows file contents in folder.
	/// </summary>
	private bool ShowFolderContents(
		FolderDto folder,
		Guid keeperId,
		PinnedBuffer dek)
	{
		if (!_sessionKeyStore.Unlock(keeperId, dek))
		{
			return false;
		}

		folder
			.ToEnumerable()
			.Concat(folder.GetAllChildren())
			.ForEach(x => x.EncryptionStatus = EncryptionStatus.Decrypted);

		return true;
	}
	#endregion
}
