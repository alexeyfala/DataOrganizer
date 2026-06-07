using Entities.Models;
using Repository.DbContexts;
using Repository.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Services;

public sealed class HotkeysRepository : RepositoryBase<HotkeyModel>, IHotkeysRepository
{
	#region Constructors
	public HotkeysRepository(SqliteDbContext context) : base(context)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task<int> RemoveRangeByOwnerIdAsync(Guid ownerId, CancellationToken token = default)
	{
		return RemoveRangeByAsync(x => x.OwnerId == ownerId, token);
	}

	/// <inheritdoc />
	public Task<int> RemoveRangeByOwnerIdsAsync(Guid[] ownerIds, CancellationToken token = default)
	{
		return RemoveRangeByAsync(x => ownerIds.Contains(x.OwnerId), token);
	}
	#endregion
}
