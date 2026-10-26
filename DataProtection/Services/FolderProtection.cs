using CommunityToolkit.Mvvm.Messaging;
using DataProtection.Enums;
using DataProtection.Helpers;
using DataProtection.Interfaces;
using EditorMessages.Extensions;
using EditorMessages.Helpers;
using Hierarchy.Dto;
using Shared.Properties;
using System.Threading;
using System.Threading.Tasks;

namespace DataProtection.Services;

/// <inheritdoc cref="IFolderProtection" />
public sealed class FolderProtection : IFolderProtection
{
	#region Data
	/// <inheritdoc cref="IFolderCipher" />
	private readonly IFolderCipher _folderCipher;

	/// <inheritdoc cref="IKeeperUnlocker" />
	private readonly IKeeperUnlocker _keeperUnlocker;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="IPasswordPrompt" />
	private readonly IPasswordPrompt _passwordPrompt;
	#endregion

	#region Constructors
	public FolderProtection(
		IFolderCipher folderCipher,
		IKeeperUnlocker keeperUnlocker,
		IMessenger messenger,
		IPasswordPrompt passwordPrompt)
	{
		_folderCipher = folderCipher;

		_keeperUnlocker = keeperUnlocker;

		_messenger = messenger;

		_passwordPrompt = passwordPrompt;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task ChangePasswordAsync(FolderDto folder, CancellationToken token = default)
	{
		if (!FolderProtectionRules.CanChangePassword(folder))
		{
			return;
		}

		using PinnedBuffer? dek = await _keeperUnlocker.RequestDekAsync(
			keeper: folder,
			header: Strings.ChangePassword,
			label: Strings.OldPassword,
			token: token).ConfigureAwait(false);

		if (dek is null)
		{
			return;
		}

		using PinnedSecret newPassword = await _passwordPrompt.RequestPasswordAsync(
			header: Strings.ChangePassword,
			label: Strings.NewPassword,
			mode: PasswordPromptMode.Create,
			token: token).ConfigureAwait(false);

		if (newPassword.IsEmpty)
		{
			return;
		}

		await _folderCipher
			.ChangePasswordAsync(
				folder,
				dek,
				newPassword,
				token)
			.ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task DecryptFolderAsync(
		FolderDto folder,
		FileDto[] files,
		CancellationToken token = default)
	{
		if (!FolderProtectionRules.CanDecrypt(folder))
		{
			return;
		}

		// Unwrapping is the password check, so a wrong password never pulls the contents into memory.
		using PinnedBuffer? dek = await _keeperUnlocker.RequestDekAsync(
			keeper: folder,
			header: Strings.DecryptFiles,
			token: token).ConfigureAwait(false);

		if (dek is null)
		{
			return;
		}

		using ProgressScope _ = _messenger.ShowProgress();

		await _folderCipher
			.DecryptFolderAsync(
				folder,
				files,
				dek,
				token)
			.ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task EncryptFolderAsync(
		FolderDto folder,
		FileDto[] files,
		CancellationToken token = default)
	{
		if (!FolderProtectionRules.CanEncrypt(folder))
		{
			return;
		}

		// A cancelled prompt returns an empty password, which changes nothing.
		using PinnedSecret password = await _passwordPrompt.RequestPasswordAsync(
			header: Strings.EncryptFiles,
			mode: PasswordPromptMode.Create,
			token: token).ConfigureAwait(false);

		await EncryptFolderAsync(
			folder,
			files,
			password,
			token).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task EncryptFolderAsync(
		FolderDto folder,
		FileDto[] files,
		PinnedSecret password,
		CancellationToken token = default)
	{
		if (password.IsEmpty || !FolderProtectionRules.CanEncrypt(folder))
		{
			return;
		}

		using ProgressScope _ = _messenger.ShowProgress();

		await _folderCipher
			.EncryptFolderAsync(
				folder,
				files,
				password,
				token)
			.ConfigureAwait(false);
	}
	#endregion
}
