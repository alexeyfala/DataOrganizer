using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Hierarchy;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Hierarchy;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Hierarchy;
using Entities.Models;
using Repository.Dto;
using Repository.Enums;
using Repository.Interfaces.Database;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Hierarchy;

public sealed class SampleSeeder : ISampleSeeder
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IEntityLoader" />
	private readonly IEntityLoader _entityLoader;

	/// <inheritdoc cref="IFolderProtection" />
	private readonly IFolderProtection _folderProtection;
	#endregion

	#region Constructors
	public SampleSeeder(
		IDbAccess dbAccess,
		IEntityLoader entityLoader,
		IFolderProtection folderProtection)
	{
		_dbAccess = dbAccess;

		_entityLoader = entityLoader;

		_folderProtection = folderProtection;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<FolderDto> SeedAsync(CancellationToken token = default)
	{
		// The database answers synchronously and the encryption is costly, so the work leaves the caller's thread.
		await Task
			.CompletedTask
			.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

		int rootIndex = await _dbAccess
			.CountOfAsync(x => x.ParentId == null, token)
			.ConfigureAwait(false);

		KeyStroke[][] takenHotkeys = await ReadHotkeysAsync(token).ConfigureAwait(false);

		SampleObjects samples = SampleHierarchy.Create(
			rootIndex,
			DateTime.Now,
			takenHotkeys);

		FolderEntity[] folders = [.. samples.Items.OfType<FolderEntity>()];

		FileEntity[] files = [.. samples.Items.OfType<FileEntity>()];

		await _dbAccess
			.AddFoldersAsync(folders, token)
			.ConfigureAwait(false);

		await _dbAccess
			.AddFilesAsync(files, token)
			.ConfigureAwait(false);

		// The folders are encrypted the way the menu does it, over a tree made of the saved objects.
		ExplorerItemDtoBase[] hierarchy = _entityLoader.Map(folders, files);

		FolderDto[] keepers = [.. hierarchy.GetFoldersBy(x => samples.KeeperIds.Contains(x.Id))];

		foreach (FolderDto keeper in keepers)
		{
			using PinnedSecret password = CreatePassword();

			await _folderProtection
				.EncryptFolderAsync(
					keeper,
					[.. keeper.Children.GetFiles()],
					password,
					token)
				.ConfigureAwait(false);
		}

		// The folder of the run is the only root of the samples.
		return hierarchy
			.OfType<FolderDto>()
			.Single();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the password of the encrypted folders in pinned storage the caller owns.
	/// </summary>
	private static PinnedSecret CreatePassword()
	{
		PinnedSecret password = new(SampleHierarchy.KeeperPassword.Length);

		SampleHierarchy
			.KeeperPassword
			.CopyTo(password.AsSpan());

		return password;
	}

	/// <summary>
	/// Returns the keys of the hotkey of a file, in the order they are pressed.
	/// </summary>
	private static KeyStroke[] ToHotkey(FileEntity file) => [.. file
		.Hotkeys
		.OrderBy(x => x.Index)
		.Select(x => new KeyStroke
		{
			Code = x.Code,
			Mask = x.Mask
		})];

	/// <summary>
	/// Returns the hotkeys of the saved files.
	/// </summary>
	private async Task<KeyStroke[][]> ReadHotkeysAsync(CancellationToken token)
	{
		FileEntity[] saved = await _dbAccess
			.GetAllFilesAsync(OptionalFileProperties.None, token)
			.ConfigureAwait(false);

		return [.. saved
			.Where(x => x.Hotkeys.Count > 0)
			.Select(ToHotkey)];
	}
	#endregion
}
