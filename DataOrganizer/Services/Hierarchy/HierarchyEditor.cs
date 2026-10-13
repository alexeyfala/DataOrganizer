using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Interfaces.Notifications;
using Entities.Enums;
using Entities.Models;
using MapsterMapper;
using Repository.Dto;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Hierarchy;

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

		string errorText = $@"{Strings.FailedToAdd} ""{name}""";

		ExplorerItemBase entity;

		try
		{
			entity = await _dbAccess
				.AddEntityAsync(parameters, token)
				.ConfigureAwait(false);
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
		string errorText = $@"{Strings.FailedToDelete} ""{dto.Name}""";

		bool result;

		try
		{
			result = await _dbAccess
				.DeleteAsync(dto.Kind, dto.Id, token)
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
		CancellationToken token = default)
	{
		if (newName.Equals(dto.Name, StringComparison.Ordinal))
		{
			string warningText = $@"{Strings.IdenticalNames} ""{newName}""";

			_notification.ShowWarningSnackbar(warningText);

			_logger.LogWarning(warningText);

			return false;
		}

		DateTime updatedAt = _timeProvider
			.GetLocalNow()
			.LocalDateTime;

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
	#endregion
}
