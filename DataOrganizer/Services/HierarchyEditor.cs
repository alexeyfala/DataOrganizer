using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Interfaces;
using Entities.Enums;
using Entities.Models;
using MapsterMapper;
using Repository.Dto;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class HierarchyEditor : IHierarchyEditor
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMapper" />
	private readonly IMapper _mapper;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public HierarchyEditor(
		IDbAccess dbAccess,
		ILogger logger,
		IMapper mapper,
		INotificationService notification)
	{
		_dbAccess = dbAccess;

		_logger = logger;

		_mapper = mapper;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<ExplorerItemDtoBase?> AddAsync(
		string name,
		EntityKind entityType,
		FolderDto? parent,
		Collection<ExplorerItemDtoBase> hierarchy,
		CancellationToken token = default)
	{
		_logger.LogInformation($"Adding a {entityType switch
		{
			EntityKind.Folder => "folder",
			EntityKind.File => "file",
			EntityKind.DataSet => "dataset",
			_ => throw new NotImplementedException()
		}} to the database.");

		AddEntityParameters parameters = new()
		{
			EntityType = entityType,
			Index = parent is not null ? parent.Children.Count : hierarchy.Count,
			Name = name,
			ParentId = parent?.Id
		};

		if (await _dbAccess
			.AddEntityAsync(parameters, token)
			.ConfigureAwait(false) is not { } entity)
		{
			string errorText = $@"{Strings.FailedToAdd} ""{name}""";

			_notification.ShowErrorSnackbar(errorText);

			_logger.LogError(errorText);

			return null;
		}

		_logger.LogInformation($"The object has been added to the database:{entity.GetPropertyValues(
			true,
			nameof(ExplorerItemBase.Id),
			nameof(ExplorerItemBase.Name),
			nameof(ExplorerItemBase.EntityType),
			nameof(ExplorerItemBase.ParentId))}");

		try
		{
			ExplorerItemDtoBase dto = _mapper.Map<ExplorerItemBase, ExplorerItemDtoBase>(entity);

			dto.Parent = parent;

			if (parent is not null)
			{
				dto.EncryptionStatus = parent.EncryptionStatus;
			}

			GetCollectionToAdd(parent, hierarchy).Add(dto);

			if (parent?.IsExpanded == false)
			{
				parent.IsExpanded = true;
			}

			string successText = $@"""{dto.Name}"" {Strings.HasBeenAdded}";

			_notification.ShowInformationSnackbar(successText);

			_logger.LogInformation(successText);

			return dto;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return null;
		}
	}

	/// <inheritdoc />
	public async Task<bool> DeleteAsync(
		ExplorerItemDtoBase dto,
		Collection<ExplorerItemDtoBase> hierarchy,
		CancellationToken token = default)
	{
		bool result = dto.EntityType switch
		{
			EntityKind.Folder => await _dbAccess.DeleteFolderAsync(dto.Id, token).ConfigureAwait(false),
			_ => await _dbAccess.DeleteFileAsync(dto.Id, token).ConfigureAwait(false)
		};

		if (!result)
		{
			string errorText = $@"{Strings.FailedToDelete} ""{dto.Name}""";

			_notification.ShowErrorSnackbar(errorText);

			_logger.LogError(errorText);

			return false;
		}

		GetCollectionToDelete(dto, hierarchy).Remove(dto);

		string text = $@"""{dto.Name}"" {Strings.HasBeenDeleted}";

		_notification.ShowInformationSnackbar(text);

		_logger.LogInformation(text);

		return true;
	}

	/// <inheritdoc />
	public async Task<bool> RenameAsync(
		ExplorerItemDtoBase dto,
		string newName,
		DateTime updatedDate,
		CancellationToken token = default)
	{
		if (newName.Equals(dto.Name, StringComparison.Ordinal))
		{
			string warningText = $@"{Strings.IdenticalNames} ""{newName}""";

			_notification.ShowWarningSnackbar(warningText);

			_logger.LogWarning(warningText);

			return false;
		}

		Task<bool> task = dto.EntityType switch
		{
			EntityKind.Folder => _dbAccess.UpdateFolderPropertiesAsync(dto.Id,
			[
				x => x.SetProperty(x => x.Name, newName),
				x => x.SetProperty(x => x.UpdatedDate, updatedDate)
			], token),
			EntityKind.File or EntityKind.DataSet => _dbAccess.UpdateFilePropertiesAsync(dto.Id,
			[
				x => x.SetProperty(x => x.Name, newName),
				x => x.SetProperty(x => x.UpdatedDate, updatedDate)
			], token),
			_ => throw new NotImplementedException()
		};

		if (!await task.ConfigureAwait(false))
		{
			string errorText = $@"{Strings.FailedToRename} ""{dto.Name}"" {Strings.To} ""{newName}""";

			_notification.ShowErrorSnackbar(errorText);

			_logger.LogError(errorText);

			return false;
		}

		string successText = $@"""{dto.Name}"" {Strings.RenamedTo} ""{newName}""";

		_notification.ShowInformationSnackbar(successText);

		_logger.LogInformation(successText);

		dto.Name = newName;

		dto.UpdatedDate = updatedDate;

		return true;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns a reference to the collection to add the object to.
	/// </summary>
	private static Collection<ExplorerItemDtoBase> GetCollectionToAdd(
		FolderDto? parent,
		Collection<ExplorerItemDtoBase> collection) => parent switch
		{
			not null => parent.Children,
			null => collection
		};

	/// <summary>
	/// Returns a reference to the collection containing the object to be removed.
	/// </summary>
	private static Collection<ExplorerItemDtoBase> GetCollectionToDelete(
		ExplorerItemDtoBase target,
		Collection<ExplorerItemDtoBase> collection) => target.Parent switch
		{
			not null => target.Parent.Children,
			null => collection
		};
	#endregion
}
