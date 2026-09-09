using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using Entities.Models;
using Mapster;
using MapsterMapper;
using Repository.Enums;
using Repository.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	public async Task<ExplorerItemDtoBase[]?> LoadFromEmbeddedDbAsync(CancellationToken token = default)
	{
		try
		{
			FolderEntity[] dbFolders = await _dbAccess
				.GetAllFoldersAsync(token)
				.ConfigureAwait(false);

			FileEntity[] dbFiles = await _dbAccess
				.GetAllFilesAsync(OptionalFileProperties.None, token)
				.ConfigureAwait(false);

			_logger.LogInformation(
				$"Number of objects loaded from the database:{Environment.NewLine}" +
				$"Folders = {dbFolders.Length},{Environment.NewLine}" +
				$"Files = {dbFiles.Length}");

			return Map(dbFolders, dbFiles);
		}
		catch (OperationCanceledException)
		{
			// A cancelled load is the caller giving up, not a database that cannot be read.
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);

			return null;
		}
	}

	/// <inheritdoc />
	public ExplorerItemDtoBase[] Map(IEnumerable<FolderEntity> dbFolders, IEnumerable<FileEntity> dbFiles)
	{
		FileDto[] dtoFiles = _mapper.Map<IEnumerable<FileEntity>, FileDto[]>(dbFiles);

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
			HotkeyDto[] orderedHotkeys = [.. file
				.Hotkeys
				.OrderBy(x => x.Index)];

			file
				.Hotkeys
				.ClearAddRange(orderedHotkeys);

			file.SetHotkeysToolTip();
		});

		ExplorerItemDtoBase[] hierarchy = _mapper
			.Map<IEnumerable<FolderEntity>, FolderDto[]>(dbFolders)
			.ToHierarchical(dtoFiles)
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

		return hierarchy;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Configures the <see cref="IMapper" />.
	/// </summary>
	private static IMapper ConfigureMapper(IMapper mapper)
	{
		TypeAdapterConfig config = mapper.Config;

		config.NewConfig<HotkeyEntity, HotkeyDto>();

		config
			.NewConfig<FileEntity, FileDto>()
			.Ignore(dest => dest.Parent!);

		config
			.NewConfig<FolderEntity, FolderDto>()
			.Ignore(dest => dest.Parent!)
			.Ignore(dest => dest.Children);

		config
			.NewConfig<ExplorerItemBase, ExplorerItemDtoBase>()
			.MapWith(src => src.GetType() == typeof(FileEntity)
				? ((FileEntity)src).Adapt<FileDto>(config)
				: ((FolderEntity)src).Adapt<FolderDto>(config));

		if (AppInfo.IsDebug)
		{
#pragma warning disable CS0168 // Variable is declared but never used
			try
			{
				config.Compile();
			}
			catch (Exception ex)
			{
				// A break under a test runner would stop a batch debug run.
				if (!AppDomain
					.CurrentDomain
					.IsRunningFromNUnit())
				{
					Debugger.Break();
				}

				// Temporarily add .IgnoreNonMapped(true) to the problematic mapping,
				// then remove one property at a time using .Map(dest => dest.PropertyName, src => src.PropertyName)
				// until you find the one that breaks the compilation.
			}
#pragma warning restore CS0168 // Variable is declared but never used
		}

		return mapper;
	}
	#endregion
}
