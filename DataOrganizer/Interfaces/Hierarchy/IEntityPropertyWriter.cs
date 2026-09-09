using DataOrganizer.Dto.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Persists individual property changes of explorer objects to the database.
/// </summary>
public interface IEntityPropertyWriter
{
	#region Methods
	/// <summary>
	/// Updates the <see cref="FolderDto.IsExpanded" /> property of a folder in the database.
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
	/// Updates the <see cref="ExplorerItemDtoBase.IsSelected" /> property of an object in the database.
	/// </summary>
	Task<bool> UpdateIsSelectedAsync(ExplorerItemDtoBase dto, CancellationToken token = default);
	#endregion
}
