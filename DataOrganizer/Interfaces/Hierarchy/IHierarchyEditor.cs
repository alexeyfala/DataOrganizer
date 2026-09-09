using DataOrganizer.Dto.Entities;
using Entities.Enums;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Hierarchy;

/// <summary>
/// Adds, renames, and deletes explorer objects in the database and in the hierarchy.
/// </summary>
public interface IHierarchyEditor
{
	#region Methods
	/// <summary>
	/// Adds an object to the database and to the hierarchy.
	/// </summary>
	Task<ExplorerItemDtoBase?> AddAsync(
		string name,
		EntityKind entityType,
		FolderDto? parent,
		Collection<ExplorerItemDtoBase> hierarchy,
		CancellationToken token = default);

	/// <summary>
	/// Deletes an object from the database and from the hierarchy.
	/// </summary>
	Task<bool> DeleteAsync(
		ExplorerItemDtoBase dto,
		Collection<ExplorerItemDtoBase> hierarchy,
		CancellationToken token = default);

	/// <summary>
	/// Renames an object in the database and in the hierarchy.
	/// </summary>
	Task<bool> RenameAsync(
		ExplorerItemDtoBase dto,
		string newName,
		DateTime updatedDate,
		CancellationToken token = default);
	#endregion
}
