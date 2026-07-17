using DataOrganizer.DTO.Entities;
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
	/// Updates the <see cref="FolderModelDto.IsExpanded" /> property of a folder in the database.
	/// </summary>
	Task<bool> UpdateIsExpandedAsync(
		Guid folderId,
		bool isExpanded,
		CancellationToken token = default);

	/// <summary>
	/// Updates the <see cref="FileModelDto.IsFavorite" /> property of a file in the database.
	/// </summary>
	Task<bool> UpdateIsFavoriteAsync(FileModelDto dto, CancellationToken token = default);

	/// <summary>
	/// Updates the <see cref="ExplorerModelBaseDto.IsSelected" /> property of an object in the database.
	/// </summary>
	Task<bool> UpdateIsSelectedAsync(ExplorerModelBaseDto dto, CancellationToken token = default);
	#endregion
}
