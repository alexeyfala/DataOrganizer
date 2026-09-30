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
	public async Task SeedAsync(CancellationToken token = default)
	{
		int rootIndex = await _dbAccess
			.CountOfAsync(x => x.ParentId == null, token)
			.ConfigureAwait(false);

		SampleObjects samples = SampleHierarchy.Create(rootIndex, DateTime.Now);

		FolderEntity[] folders = [.. samples.Items.OfType<FolderEntity>()];

		FileEntity[] files = [.. samples.Items.OfType<FileEntity>()];

		await DropTakenHotkeysAsync(files, token).ConfigureAwait(false);

		await _dbAccess
			.AddFoldersAsync(folders, token)
			.ConfigureAwait(false);

		await _dbAccess
			.AddFilesAsync(files, token)
			.ConfigureAwait(false);

		// The folders are encrypted the way the menu does it, over a tree made of the saved objects.
		FolderDto[] keepers = [.. _entityLoader
			.Map(folders, files)
			.GetFoldersBy(x => samples.KeeperIds.Contains(x.Id))];

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
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when one hotkey is a run of keys inside the other, so that typing the longer one fires both.
	/// </summary>
	private static bool Clashes(KeyStroke[] first, KeyStroke[] second)
	{
		return first.Length >= second.Length
			? first.AsSpan().IndexOf(second) >= 0
			: second.AsSpan().IndexOf(first) >= 0;
	}

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
	/// Takes the hotkey away from every file whose hotkey clashes with one of a saved file, which keeps it.
	/// </summary>
	private async Task DropTakenHotkeysAsync(FileEntity[] files, CancellationToken token)
	{
		FileEntity[] saved = await _dbAccess
			.GetAllFilesAsync(OptionalFileProperties.None, token)
			.ConfigureAwait(false);

		KeyStroke[][] taken = [.. saved
			.Where(x => x.Hotkeys.Count > 0)
			.Select(ToHotkey)];

		foreach (FileEntity file in files.Where(x => x.Hotkeys.Count > 0))
		{
			KeyStroke[] hotkey = ToHotkey(file);

			if (taken.Any(x => Clashes(x, hotkey)))
			{
				file
					.Hotkeys
					.Clear();
			}
		}
	}
	#endregion
}
