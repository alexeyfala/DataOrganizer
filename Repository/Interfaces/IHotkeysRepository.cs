using Entities.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interfaces;

/// <summary>
/// Repository for <see cref="HotkeyModel" />.
/// </summary>
public interface IHotkeysRepository
{
	#region Methods
	/// <inheritdoc cref="RepositoryBase{T}.AddAsync" />
	ValueTask<EntityEntry<HotkeyModel>> AddAsync(HotkeyModel entity, CancellationToken token = default);

	/// <summary>
	/// Removes entities from the database by owner ID.
	/// </summary>
	Task<int> RemoveRangeByOwnerIdAsync(Guid ownerId, CancellationToken token = default);

	/// <summary>
	/// Removes entities from the database by owner IDs.
	/// </summary>
	Task<int> RemoveRangeByOwnerIdsAsync(Guid[] ownerIds, CancellationToken token = default);
	#endregion
}
