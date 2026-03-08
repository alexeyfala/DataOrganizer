using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using Entities.Abstract;
using Entities.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Repository.DbContexts;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

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

		_mapper = ConfigureMapper(mapper);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public LoadFromDbResult LoadFromDb(string dataSource)
	{
		using SqliteDbContext context = _dbAccess.GetSQliteDbContext(dataSource);

		FolderModel[] dbFolders = [.. context
			.Set<FolderModel>()
			.AsNoTracking()];

		FileModel[] dbFiles = [.. context
			.Set<FileModel>()
			.AsNoTracking()];

		_dbAccess.ClearPool(context);

		return new()
		{
			Files = dbFiles,
			Folders = dbFolders
		};
	}

	/// <inheritdoc />
	public async Task<ExplorerModelBaseDto[]> LoadFromEmbeddedDbAsync(CancellationToken token = default)
	{
		try
		{
			FolderModel[] dbFolders = await _dbAccess
				.GetAllFoldersAsync(token: token)
				.ConfigureAwait(false);

			string[] excluded =
			[
				nameof(FileModel.Contents),
				nameof(FileModel.Properties)
			];

			FileModel[] dbFiles = await _dbAccess
				.GetAllFilesAsync(token: token, excludedProperties: excluded)
				.ConfigureAwait(false);

			_logger.LogInformation(
				$"Number of objects loaded from the database:{Environment.NewLine}" +
				$"Folders = {dbFolders.Length},{Environment.NewLine}" +
				$"Files = {dbFiles.Length}");

			return Map(dbFolders, dbFiles);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return [];
		}
	}

	/// <inheritdoc />
	public ExplorerModelBaseDto[] Map(IEnumerable<FolderModel> dbFolders, IEnumerable<FileModel> dbFiles)
	{
		FileModelDto[] dtoFiles = _mapper.Map<IEnumerable<FileModel>, FileModelDto[]>(dbFiles);

		dtoFiles.ForEach(file =>
		{
			if (file
				.Hotkeys
				.Count == 0)
			{
				return;
			}

			// After importing from JSON or XML, or adding to the database, the order of hotkeys is broken,
			// so it needs to be restored.
			HotkeyModelDto[] orderedHotkeys = [.. file
				.Hotkeys
				.OrderBy(x => x.Index)];

			file
				.Hotkeys
				.ClearAddRange(orderedHotkeys);

			file.SetHotkeysToolTip();
		});

		ExplorerModelBaseDto[] hierarchy = _mapper
			.Map<IEnumerable<FolderModel>, FolderModelDto[]>(dbFolders)
			.ToHierarchical(dtoFiles)
			.ToArray()
			.SortByIndexRecursively();

		hierarchy
			.GetFoldersBy(x => !string.IsNullOrEmpty(x.PasswordHash))
			.ForEach(folder =>
			{
				const EncryptionStatus status = EncryptionStatus.Encrypted;

				folder.EncryptionStatus = status;

				folder
					.GetAllChildren()
					.ForEach(x => x.EncryptionStatus = status);
			});

		return hierarchy;
	}
	#endregion

	#region Service
	/// <summary>
	/// Configures the <see cref="IMapper" />.
	/// </summary>
	private static IMapper ConfigureMapper(IMapper mapper)
	{
		mapper.Config
			.NewConfig<ExplorerModelBase, ExplorerModelBaseDto>()
			.Include<FileModel, FileModelDto>()
			.Include<FolderModel, FolderModelDto>();

		return mapper;
	}
	#endregion
}
