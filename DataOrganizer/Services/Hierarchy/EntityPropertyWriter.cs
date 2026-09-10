using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces.Hierarchy;
using Entities.Enums;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Hierarchy;

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
		const string propertyName = nameof(FolderDto.IsExpanded);

		_logger.LogDebug(
			$@"Update ""{propertyName}"" property in of folder ""{folderId}"" in database is requested");

		return _dbAccess.UpdateFolderPropertiesAsync(folderId,
		[
			x => x.SetProperty(x => x.IsExpanded, isExpanded)
		], token);
	}

	/// <inheritdoc />
	public Task<bool> UpdateIsFavoriteAsync(FileDto dto, CancellationToken token = default)
	{
		const string propertyName = nameof(FileDto.IsFavorite);

		_logger.LogDebug($@"Update ""{propertyName}"" property in database is requested:{dto.GetPropertyValues(
			true,
			nameof(ExplorerItemDtoBase.EntityType),
			nameof(ExplorerItemDtoBase.Name),
			propertyName)}");

		return _dbAccess.UpdateFilePropertiesAsync(dto.Id,
		[
			x => x.SetProperty(x => x.IsFavorite, dto.IsFavorite)
		], token);
	}

	/// <inheritdoc />
	public Task<bool> UpdateIsSelectedAsync(
		ExplorerItemDtoBase dto,
		CancellationToken token = default)
	{
		const string propertyName = nameof(ExplorerItemDtoBase.IsSelected);

		_logger.LogDebug($@"Update ""{propertyName}"" property in database is requested:{dto.GetPropertyValues(
			true,
			nameof(ExplorerItemDtoBase.EntityType),
			nameof(ExplorerItemDtoBase.Name),
			propertyName)}");

		return dto.EntityType switch
		{
			EntityKind.Folder => _dbAccess.UpdateFolderPropertiesAsync(dto.Id,
			[
				x => x.SetProperty(x => x.IsSelected, dto.IsSelected)
			], token),
			EntityKind.File or EntityKind.Dataset => _dbAccess.UpdateFilePropertiesAsync(dto.Id,
			[
				x => x.SetProperty(x => x.IsSelected, dto.IsSelected)
			], token),
			_ => throw new NotImplementedException()
		};
	}
	#endregion
}
