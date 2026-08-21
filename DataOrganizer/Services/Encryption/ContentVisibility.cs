using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
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

	/// <inheritdoc cref="ISessionKeyStore" />
	private readonly ISessionKeyStore _sessionKeyStore;
	#endregion

	#region Constructors
	public ContentVisibility(
		IEncryptionFailureReporter failureReporter,
		IKeeperUnlocker keeperUnlocker,
		IMessenger messenger,
		ISessionKeyStore sessionKeyStore)
	{
		_failureReporter = failureReporter;

		_keeperUnlocker = keeperUnlocker;

		_messenger = messenger;

		_sessionKeyStore = sessionKeyStore;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void HideAllContents(IEnumerable<ExplorerModelBaseDto> hierarchy)
	{
		hierarchy
			.FilterBy(x => x.EncryptionStatus == EncryptionStatus.Decrypted)
			.ForEach(x => x.EncryptionStatus = EncryptionStatus.Encrypted);

		_sessionKeyStore.LockAll();
	}

	/// <inheritdoc />
	public void HideFileContents(FileModelDto file)
	{
		file.EncryptionStatus = EncryptionStatus.Encrypted;

		LockKeeperOf(file);
	}

	/// <inheritdoc />
	public void HideFolderContents(FolderModelDto folder)
	{
		folder
			.ToEnumerable()
			.Concat(folder.GetAllChildren())
			.ForEach(x => x.EncryptionStatus = EncryptionStatus.Encrypted);

		LockKeeperOf(folder);
	}

	/// <inheritdoc />
	public async Task<bool> ShowFileContentsAsync(FileModelDto file, CancellationToken token = default)
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
				_messenger.ShowSnackbar(Strings.FailedToShowFileContents, SnackbarMessageLevel.Error);

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
	public async Task ShowFolderContentsAsync(FolderModelDto folder, CancellationToken token = default)
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

			_messenger.ShowSnackbar(Strings.FailedToShowFileContents, SnackbarMessageLevel.Error);
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
	private void LockKeeperOf(ExplorerModelBaseDto item)
	{
		FolderModelDto? keeper = item.FindPasswordKeeper();

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
		FolderModelDto folder,
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
