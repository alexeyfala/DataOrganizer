using DataOrganizer.DTO.Entities;
using DataOrganizer.Interfaces;
using Entities.Enums;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class EntityPropertyWriter : IEntityPropertyWriter
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public EntityPropertyWriter(
		IDbAccess dbAccess,
		ILogger logger)
	{
		_dbAccess = dbAccess;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task<bool> UpdateIsExpandedAsync(
		Guid folderId,
		bool isExpanded,
		CancellationToken token = default)
	{
		const string propertyName = nameof(FolderModelDto.IsExpanded);

		_logger.LogDebug(
			$@"Update ""{propertyName}"" property in of folder ""{folderId}"" in database is requested");

		return _dbAccess.UpdateFolderPropertiesAsync(folderId,
		[
			x => x.SetProperty(x => x.IsExpanded, isExpanded)
		], token);
	}

	/// <inheritdoc />
	public Task<bool> UpdateIsFavoriteAsync(FileModelDto dto, CancellationToken token = default)
	{
		const string propertyName = nameof(FileModelDto.IsFavorite);

		_logger.LogDebug($@"Update ""{propertyName}"" property in database is requested:{dto.GetPropertyValues(
			true,
			nameof(ExplorerModelBaseDto.EntityType),
			nameof(ExplorerModelBaseDto.Name),
			propertyName)}");

		return _dbAccess.UpdateFilePropertiesAsync(dto.Id,
		[
			x => x.SetProperty(x => x.IsFavorite, dto.IsFavorite)
		], token);
	}

	/// <inheritdoc />
	public Task<bool> UpdateIsSelectedAsync(
		ExplorerModelBaseDto dto,
		CancellationToken token = default)
	{
		const string propertyName = nameof(ExplorerModelBaseDto.IsSelected);

		_logger.LogDebug($@"Update ""{propertyName}"" property in database is requested:{dto.GetPropertyValues(
			true,
			nameof(ExplorerModelBaseDto.EntityType),
			nameof(ExplorerModelBaseDto.Name),
			propertyName)}");

		return dto.EntityType switch
		{
			EntityType.Folder => _dbAccess.UpdateFolderPropertiesAsync(dto.Id,
			[
				x => x.SetProperty(x => x.IsSelected, dto.IsSelected)
			], token),
			EntityType.File or EntityType.DataSet => _dbAccess.UpdateFilePropertiesAsync(dto.Id,
			[
				x => x.SetProperty(x => x.IsSelected, dto.IsSelected)
			], token),
			_ => throw new NotImplementedException()
		};
	}
	#endregion
}
