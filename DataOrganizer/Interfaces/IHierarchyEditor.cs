using DataOrganizer.DTO.Entities;
using Entities.Enums;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Adds, renames, and deletes explorer objects in the database and in the hierarchy.
/// </summary>
public interface IHierarchyEditor
{
	#region Methods
	/// <summary>
	/// Adds an object to the database and to the hierarchy.
	/// </summary>
	Task<ExplorerModelBaseDto?> AddAsync(
		string name,
		EntityType entityType,
		FolderModelDto? parent,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token = default);

	/// <summary>
	/// Deletes an object from the database and from the hierarchy.
	/// </summary>
	Task<bool> DeleteAsync(
		ExplorerModelBaseDto dto,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token = default);

	/// <summary>
	/// Renames an object in the database and in the hierarchy.
	/// </summary>
	Task<bool> RenameAsync(
		ExplorerModelBaseDto dto,
		string newName,
		DateTime updatedDate,
		CancellationToken token = default);
	#endregion
}
