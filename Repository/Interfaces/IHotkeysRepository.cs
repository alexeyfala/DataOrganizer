using Entities.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Repository.Abstract;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interfaces;

/// <summary>
/// Repository for <see cref="HotkeyModel" />.
/// </summary>
public interface IHotkeysRepository
{
	#region Methods
	/// <inheritdoc cref="RepositoryBase{T}.AddAsync(T, CancellationToken)" />
	ValueTask<EntityEntry<HotkeyModel>> AddAsync(HotkeyModel entity, CancellationToken token);

	/// <summary>
	/// Returns a flat list of <see cref="HotkeyModel" /> entities according to a condition from the database.
	/// </summary>
	Task<HotkeyModel[]> GetAsync(
		Expression<Func<HotkeyModel, bool>> condition,
		bool trackChanges = false,
		CancellationToken token = default);

	/// <inheritdoc cref="RepositoryBase{T}.RemoveRange(IEnumerable{T})" />
	void RemoveRange(IEnumerable<HotkeyModel> entities);
	#endregion
}
