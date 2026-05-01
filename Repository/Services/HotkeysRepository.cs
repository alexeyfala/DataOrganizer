using Entities.Models;
using Repository.Abstract;
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
	#endregion
}
