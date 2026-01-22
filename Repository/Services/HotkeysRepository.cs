using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Repository.Abstract;
using Repository.DbContexts;
using Repository.Interfaces;
using System;
using System.Linq.Expressions;
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
	public Task<HotkeyModel[]> GetAsync(
		Expression<Func<HotkeyModel, bool>> condition,
		bool trackChanges = false,
		CancellationToken token = default)
	{
		return FindBy(condition, trackChanges).ToArrayAsync(token);
	}
	#endregion
}
