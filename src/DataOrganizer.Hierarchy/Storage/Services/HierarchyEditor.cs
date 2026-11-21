using DataOrganizer.Database.Entities.Enums;
using DataOrganizer.Database.Entities.Models;
using DataOrganizer.Database.Repository.Dto;
using DataOrganizer.Database.Repository.Interfaces.Database;
using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.Extensions;
using DataOrganizer.Hierarchy.Storage.Interfaces;
using DataOrganizer.Infrastructure.Diagnostics.Interfaces;
using DataOrganizer.Infrastructure.Notifications.Interfaces;
using DataOrganizer.Shared.Extensions;
using DataOrganizer.Shared.Properties;
using MapsterMapper;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Hierarchy.Storage.Services;

/// <inheritdoc cref="IHierarchyEditor" />
public sealed class HierarchyEditor : IHierarchyEditor
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDbFailureReporter" />
	private readonly IDbFailureReporter _dbFailureReporter;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMapper" />
	private readonly IMapper _mapper;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;
	#endregion

	#region Constructors
	public HierarchyEditor(
		IDbAccess dbAccess,
		IDbFailureReporter dbFailureReporter,
		ILogger logger,
		IMapper mapper,
		INotificationService notification,
		TimeProvider timeProvider)
	{
		_dbAccess = dbAccess;

		_dbFailureReporter = dbFailureReporter;

		_logger = logger;

		_mapper = mapper;

		_notification = notification;

		_timeProvider = timeProvider;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<ExplorerItemDtoBase?> AddAsync(
		string name,
		EntityKind kind,
		FolderDto? parent,
		Collection<ExplorerItemDtoBase> hierarchy,
		CancellationToken token = default)
	{
		_logger.LogInformation($"Adding a {kind switch
		{
			EntityKind.Folder => "folder",
			EntityKind.File => "file",
			EntityKind.Dataset => "dataset",
			_ => throw new NotImplementedException()
		}} to the database.");

		AddEntityParameters parameters = new()
		{
			Index = parent is not null ? parent.Children.Count : hierarchy.Count,
			Kind = kind,
			Name = name,
			ParentId = parent?.Id
		};

		// Stryker disable once String : wording of a message
		string errorText = $@"{Strings.FailedToAdd} ""{name}""";

		ExplorerItemBase entity;

		try
		{
			// The hierarchy may be bound to the UI, so it changes in the context of the caller.
			entity = await _dbAccess
				.AddEntityAsync(parameters, token)
				.ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			_dbFailureReporter.Report(ex, errorText);

			return null;
		}

		_logger.LogInformation($"The object has been added to the database:{entity.GetPropertyValues(
			true,
			nameof(ExplorerItemBase.Id),
			nameof(ExplorerItemBase.Name),
			nameof(ExplorerItemBase.Kind),
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

			// Stryker disable once String : wording of a message
			string successText = $@"""{dto.Name}"" {Strings.HasBeenAdded}";

			_notification.ShowInformationSnackbar(successText);

			_logger.LogInformation(successText);

			return dto;
		}
		// Stryker disable once Block : the block only logs and returns what an empty block returns
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
		if (!await DeleteEntityAsync(
			dto,
			hierarchy,
			token).ConfigureAwait(false))
		{
			return false;
		}

		// Stryker disable once String : wording of a message
		ShowDeleted($@"""{dto.Name}"" {Strings.HasBeenDeleted}");

		return true;
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<ExplorerItemDtoBase>> DeleteManyAsync(
		IReadOnlyList<ExplorerItemDtoBase> items,
		Collection<ExplorerItemDtoBase> hierarchy,
		CancellationToken token = default)
	{
		List<ExplorerItemDtoBase> deleted = [];

		foreach (ExplorerItemDtoBase item in items)
		{
			// The first failure is reported and ends the deletion, as the database is likely out of reach.
			if (!await DeleteEntityAsync(
				item,
				hierarchy,
				token).ConfigureAwait(true))
			{
				break;
			}

			deleted.Add(item);
		}

		if (deleted.Count == 0)
		{
			return deleted;
		}

		// Stryker disable once String : wording of a message
		ShowDeleted($@"""{deleted.GetCount().AsString()}"" {Strings.HaveBeenDeleted}");

		return deleted;
	}

	/// <inheritdoc />
	public async Task<bool> RenameAsync(
		ExplorerItemDtoBase dto,
		string newName,
		CancellationToken token = default)
	{
		if (newName.Equals(dto.Name, StringComparison.Ordinal))
		{
			// Stryker disable once String : wording of a message
			string warningText = $@"{Strings.IdenticalNames} ""{newName}""";

			_notification.ShowWarningSnackbar(warningText);

			_logger.LogWarning(warningText);

			return false;
		}

		DateTime updatedAt = _timeProvider
			.GetLocalNow()
			.LocalDateTime;

		// Stryker disable once String : wording of a message
		string errorText = $@"{Strings.FailedToRename} ""{dto.Name}"" {Strings.To} ""{newName}""";

		bool result;

		try
		{
			result = await _dbAccess
				.RenameAsync(dto.Kind, dto.Id, newName, updatedAt, token)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_dbFailureReporter.Report(ex, errorText);

			return false;
		}

		if (!result)
		{
			_notification.ShowErrorSnackbar(errorText);

			_logger.LogError(errorText);

			return false;
		}

		// Stryker disable once String : wording of a message
		string successText = $@"""{dto.Name}"" {Strings.RenamedTo} ""{newName}""";

		_notification.ShowInformationSnackbar(successText);

		_logger.LogInformation(successText);

		dto.Name = newName;

		dto.UpdatedAt = updatedAt;

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

	/// <summary>
	/// Deletes an object from the database and from the hierarchy; a failure is reported and gives <c>false</c>.
	/// </summary>
	private async Task<bool> DeleteEntityAsync(
		ExplorerItemDtoBase dto,
		Collection<ExplorerItemDtoBase> hierarchy,
		CancellationToken token)
	{
		// Stryker disable once String : wording of a message
		string errorText = $@"{Strings.FailedToDelete} ""{dto.Name}""";

		bool result;

		try
		{
			// The hierarchy may be bound to the UI, so it changes in the context of the caller.
			result = await _dbAccess
				.DeleteAsync(dto.Kind, dto.Id, token)
				.ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			_dbFailureReporter.Report(ex, errorText);

			return false;
		}

		if (!result)
		{
			_notification.ShowErrorSnackbar(errorText);

			_logger.LogError(errorText);

			return false;
		}

		GetCollectionToDelete(dto, hierarchy).Remove(dto);

		return true;
	}

	/// <summary>
	/// Tells about a finished deletion.
	/// </summary>
	private void ShowDeleted(string text)
	{
		_notification.ShowInformationSnackbar(text);

		_logger.LogInformation(text);
	}
	#endregion
}
