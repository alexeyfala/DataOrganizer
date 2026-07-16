using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Messages;
using Entities.Enums;
using Entities.Models;
using MapsterMapper;
using Repository.DTO;
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

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;
	#endregion

	#region Constructors
	public HierarchyEditor(
		IDbAccess dbAccess,
		ILogger logger,
		IMapper mapper,
		IMessenger messenger)
	{
		_dbAccess = dbAccess;

		_logger = logger;

		_mapper = mapper;

		_messenger = messenger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<ExplorerModelBaseDto?> AddAsync(
		string name,
		EntityType entityType,
		FolderModelDto? parent,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token = default)
	{
		_logger.LogInformation($"Adding a {entityType switch
		{
			EntityType.Folder => "folder",
			EntityType.File => "file",
			EntityType.DataSet => "dataset",
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

			Notify(errorText, SnackbarMessageLevel.Error);

			_logger.LogError(errorText);

			return null;
		}

		_logger.LogInformation($"The object has been added to the database:{entity.GetPropertyValues(
			true,
			nameof(ExplorerModelBase.Id),
			nameof(ExplorerModelBase.Name),
			nameof(ExplorerModelBase.EntityType),
			nameof(ExplorerModelBase.ParentId))}");

		try
		{
			ExplorerModelBaseDto dto = _mapper.Map<ExplorerModelBase, ExplorerModelBaseDto>(entity);

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

			Notify(successText, SnackbarMessageLevel.Information);

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
		ExplorerModelBaseDto dto,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token = default)
	{
		bool result = dto.EntityType switch
		{
			EntityType.Folder => await _dbAccess.DeleteFolderAsync(dto.Id, token).ConfigureAwait(false),
			_ => await _dbAccess.DeleteFileAsync(dto.Id, token).ConfigureAwait(false)
		};

		if (!result)
		{
			string errorText = $@"{Strings.FailedToDelete} ""{dto.Name}""";

			Notify(errorText, SnackbarMessageLevel.Error);

			_logger.LogError(errorText);

			return false;
		}

		GetCollectionToDelete(dto, hierarchy).Remove(dto);

		string text = $@"""{dto.Name}"" {Strings.HasBeenDeleted}";

		Notify(text, SnackbarMessageLevel.Information);

		_logger.LogInformation(text);

		return true;
	}

	/// <inheritdoc />
	public async Task<bool> RenameAsync(
		ExplorerModelBaseDto dto,
		string newName,
		DateTime updatedDate,
		CancellationToken token = default)
	{
		if (newName.Equals(dto.Name, StringComparison.Ordinal))
		{
			string warningText = $@"{Strings.IdenticalNames} ""{newName}""";

			Notify(warningText, SnackbarMessageLevel.Warning);

			_logger.LogWarning(warningText);

			return false;
		}

		Task<bool> task = dto.EntityType switch
		{
			EntityType.Folder => _dbAccess.UpdateFolderPropertiesAsync(dto.Id,
			[
				x => x.SetProperty(x => x.Name, newName),
				x => x.SetProperty(x => x.UpdatedDate, updatedDate)
			], token),
			EntityType.File or EntityType.DataSet => _dbAccess.UpdateFilePropertiesAsync(dto.Id,
			[
				x => x.SetProperty(x => x.Name, newName),
				x => x.SetProperty(x => x.UpdatedDate, updatedDate)
			], token),
			_ => throw new NotImplementedException()
		};

		if (!await task.ConfigureAwait(false))
		{
			string errorText = $@"{Strings.FailedToRename} ""{dto.Name}"" {Strings.To} ""{newName}""";

			Notify(errorText, SnackbarMessageLevel.Error);

			_logger.LogError(errorText);

			return false;
		}

		string successText = $@"""{dto.Name}"" {Strings.RenamedTo} ""{newName}""";

		Notify(successText, SnackbarMessageLevel.Information);

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
	private static Collection<ExplorerModelBaseDto> GetCollectionToAdd(
		FolderModelDto? parent,
		Collection<ExplorerModelBaseDto> collection) => parent switch
		{
			not null => parent.Children,
			null => collection
		};

	/// <summary>
	/// Returns a reference to the collection containing the object to be removed.
	/// </summary>
	private static Collection<ExplorerModelBaseDto> GetCollectionToDelete(
		ExplorerModelBaseDto target,
		Collection<ExplorerModelBaseDto> collection) => target.Parent switch
		{
			not null => target.Parent.Children,
			null => collection
		};

	/// <summary>
	/// Sends a snackbar notification.
	/// </summary>
	private void Notify(string text, SnackbarMessageLevel level) => _messenger.Send(new ShowSnackbarMessage(text, level));
	#endregion
}
