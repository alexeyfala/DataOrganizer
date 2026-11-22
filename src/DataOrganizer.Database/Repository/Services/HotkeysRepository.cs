using DataOrganizer.Database.Entities.Models;
using DataOrganizer.Database.Repository.DbContexts;
using DataOrganizer.Database.Repository.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Database.Repository.Services;

/// <inheritdoc cref="IHotkeysRepository" />
[ExcludeFromCodeCoverage(Justification = "A thin wrapper over EF Core")]
public sealed class HotkeysRepository : RepositoryBase<HotkeyEntity>, IHotkeysRepository
{
	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="HotkeysRepository" /> class.
	/// </summary>
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
