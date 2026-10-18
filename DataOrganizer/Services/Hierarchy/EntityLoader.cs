using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions.Hierarchy;
using DataOrganizer.Interfaces.Hierarchy;
using Entities.Models;
using MapsterMapper;
using Repository.Enums;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Hierarchy;

public sealed class EntityLoader : IEntityLoader
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <summary>
	/// Mapper.
	/// </summary>
	private readonly IMapper _mapper;
	#endregion

	#region Constructors
	public EntityLoader(
		IDbAccess dbAccess,
		ILogger logger,
		IMapper mapper)
	{
		_dbAccess = dbAccess;

		_logger = logger;

		_mapper = mapper;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<LoadedHierarchy?> LoadHierarchyAsync(CancellationToken token = default)
	{
		try
		{
			FolderEntity[] folders = await _dbAccess
				.GetAllFoldersAsync(token)
				.ConfigureAwait(false);

			FileEntity[] files = await _dbAccess
				.GetAllFilesAsync(OptionalFileProperties.None, token)
				.ConfigureAwait(false);

			_logger.LogInformation(
				$"Number of objects loaded from the database:{Environment.NewLine}" +
				$"Folders = {folders.Length},{Environment.NewLine}" +
				$"Files = {files.Length}");

			return Map(folders, files);
		}
		catch (OperationCanceledException)
		{
			// A cancelled load is the caller giving up, not a database that cannot be read.
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			return null;
		}
	}

	/// <inheritdoc />
	public LoadedHierarchy Map(IReadOnlyCollection<FolderEntity> folders, IReadOnlyCollection<FileEntity> files)
	{
		FileDto[] fileDtos = _mapper.Map<IEnumerable<FileEntity>, FileDto[]>(files);

		fileDtos.ForEach(file =>
		{
			if (file
				.Hotkeys
				.Count == 0)
			{
				return;
			}

			// After importing from JSON or XML, or adding to the database, the order of hotkeys is broken,
			// so it needs to be restored.
			HotkeyDto[] orderedHotkeys = [.. file
				.Hotkeys
				.OrderBy(x => x.Index)];

			file
				.Hotkeys
				.ClearAddRange(orderedHotkeys);
		});

		ExplorerItemDtoBase[] hierarchy = _mapper
			.Map<IEnumerable<FolderEntity>, FolderDto[]>(folders)
			.ToHierarchical(fileDtos)
			.ToArray()
			.SortByIndexRecursively();

		hierarchy
			.GetFoldersBy(x => x.IsPasswordKeeper())
			.ForEach(folder =>
			{
				const EncryptionStatus status = EncryptionStatus.Encrypted;

				folder.EncryptionStatus = status;

				folder
					.GetAllChildren()
					.ForEach(x => x.EncryptionStatus = status);
			});

		return new()
		{
			ExpandedIds = [.. folders
				.Where(static x => x.IsExpanded)
				.Select(static x => x.Id)],
			Items = hierarchy,
			SelectedIds = [.. folders
				.Concat<ExplorerItemBase>(files)
				.Where(static x => x.IsSelected)
				.Select(static x => x.Id)]
		};
	}
	#endregion
}
