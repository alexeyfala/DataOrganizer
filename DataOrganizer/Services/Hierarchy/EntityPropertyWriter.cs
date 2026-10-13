using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces.Hierarchy;
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
		return _dbAccess.UpdateIsExpandedAsync(folderId, isExpanded, token);
	}

	/// <inheritdoc />
	public Task<bool> UpdateIsFavoriteAsync(FileDto dto, CancellationToken token = default)
	{
		const string propertyName = nameof(FileDto.IsFavorite);

		_logger.LogDebug($@"Update ""{propertyName}"" property in database is requested:{dto.GetPropertyValues(
			true,
			nameof(ExplorerItemDtoBase.Kind),
			nameof(ExplorerItemDtoBase.Name),
			propertyName)}");

		return _dbAccess.UpdateIsFavoriteAsync(dto.Id, dto.IsFavorite, token);
	}

	/// <inheritdoc />
	public Task<bool> UpdateIsSelectedAsync(
		ExplorerItemDtoBase dto,
		CancellationToken token = default)
	{
		const string propertyName = nameof(ExplorerItemDtoBase.IsSelected);

		_logger.LogDebug($@"Update ""{propertyName}"" property in database is requested:{dto.GetPropertyValues(
			true,
			nameof(ExplorerItemDtoBase.Kind),
			nameof(ExplorerItemDtoBase.Name),
			propertyName)}");

		return _dbAccess.UpdateIsSelectedAsync(
			dto.Kind,
			dto.Id,
			dto.IsSelected,
			token);
	}
	#endregion
}
