using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using Entities.Abstract;
using Entities.Models;
using MapsterMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Repository.DbContexts;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using System;
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
	public ExplorerModelBaseDto[] LoadFromDb(string dataSource)
	{
		try
		{
			SqliteConnectionStringBuilder builder = new()
			{
				DataSource = dataSource
			};

			DbContextOptions<SqliteDbContext> options = new DbContextOptionsBuilder<SqliteDbContext>()
				.UseSqlite(builder.ToString())
				.Options;

			SqliteDbContext context = new(options);

			ExplorerModelBase[] entities = [.. context.Set<ExplorerModelBase>()];

			using SqliteConnection connection = (SqliteConnection)context
				.Database
				.GetDbConnection();

			SqliteConnection.ClearPool(connection);

			return Map(
				[.. entities.OfType<FolderModel>()],
				[.. entities.OfType<FileModel>()]);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return [];
		}
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

	/// <summary>
	/// Maps entities from the database to DTO objects.
	/// </summary>
	private ExplorerModelBaseDto[] Map(FolderModel[] dbFolders, FileModel[] dbFiles)
	{
		FileModelDto[] dtoFiles = _mapper.Map<FileModel[], FileModelDto[]>(dbFiles);

		dtoFiles.ForEach(dto =>
		{
			if (dto
				.Hotkeys
				.Count == 0)
			{
				return;
			}

			dto.SetHotkeysToolTip();
		});

		ExplorerModelBaseDto[] hierarchy = _mapper
			.Map<FolderModel[], FolderModelDto[]>(dbFolders)
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
}
