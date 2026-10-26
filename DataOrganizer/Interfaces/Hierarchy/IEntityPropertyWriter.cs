using Entities.Models;
using Hierarchy.Dto;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Hierarchy;

/// <summary>
/// Persists individual property changes of explorer objects to the database.
/// </summary>
public interface IEntityPropertyWriter
{
	#region Methods
	/// <summary>
	/// Updates the <see cref="FolderEntity.IsExpanded" /> property of a folder in the database.
	/// </summary>
	Task<bool> UpdateIsExpandedAsync(
		Guid folderId,
		bool isExpanded,
		CancellationToken token = default);

	/// <summary>
	/// Updates the <see cref="FileDto.IsFavorite" /> property of a file in the database.
	/// </summary>
	Task<bool> UpdateIsFavoriteAsync(FileDto dto, CancellationToken token = default);

	/// <summary>
	/// Updates the <see cref="ExplorerItemBase.IsSelected" /> property of an object in the database.
	/// </summary>
	Task<bool> UpdateIsSelectedAsync(
		ExplorerItemDtoBase dto,
		bool isSelected,
		CancellationToken token = default);
	#endregion
}
