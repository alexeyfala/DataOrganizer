using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Repository.Abstract;
using Repository.DbContexts;
using Repository.Enums;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Services;

public sealed class FileRepository : RepositoryBase<FileModel>, IFileRepository
{
	#region Constructors
	public FileRepository(SqliteDbContext context) : base(context)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task<FileModel[]> GetAllAsync(OptionalFileProperty optionalProperties, CancellationToken token = default)
	{
		bool includeContents = optionalProperties.HasFlag(OptionalFileProperty.Contents);

		bool includeProperties = optionalProperties.HasFlag(OptionalFileProperty.Properties);

		return (includeContents, includeProperties) switch
		{
			(false, false) => FindAll().Select(x => new FileModel
			{
				CreatedDate = x.CreatedDate,
				EntityType = x.EntityType,
				Hotkeys = x.Hotkeys,
				Id = x.Id,
				Index = x.Index,
				IsFavorite = x.IsFavorite,
				IsSelected = x.IsSelected,
				Name = x.Name,
				Note = x.Note,
				ParentId = x.ParentId,
				UpdatedDate = x.UpdatedDate
			}).ToArrayAsync(token),
			(true, false) => FindAll().Select(x => new FileModel
			{
				Contents = x.Contents, // ← Include.
				CreatedDate = x.CreatedDate,
				EntityType = x.EntityType,
				Hotkeys = x.Hotkeys,
				Id = x.Id,
				Index = x.Index,
				IsFavorite = x.IsFavorite,
				IsSelected = x.IsSelected,
				Name = x.Name,
				Note = x.Note,
				ParentId = x.ParentId,
				UpdatedDate = x.UpdatedDate
			}).ToArrayAsync(token),
			(false, true) => FindAll().Select(x => new FileModel
			{
				CreatedDate = x.CreatedDate,
				EntityType = x.EntityType,
				Hotkeys = x.Hotkeys,
				Id = x.Id,
				Index = x.Index,
				IsFavorite = x.IsFavorite,
				IsSelected = x.IsSelected,
				Name = x.Name,
				Note = x.Note,
				ParentId = x.ParentId,
				Properties = x.Properties, // ← Include.
				UpdatedDate = x.UpdatedDate
			}).ToArrayAsync(token),
			(true, true) => FindAll().Select(x => new FileModel
			{
				Contents = x.Contents, // ← Include.
				CreatedDate = x.CreatedDate,
				EntityType = x.EntityType,
				Hotkeys = x.Hotkeys,
				Id = x.Id,
				Index = x.Index,
				IsFavorite = x.IsFavorite,
				IsSelected = x.IsSelected,
				Name = x.Name,
				Note = x.Note,
				ParentId = x.ParentId,
				Properties = x.Properties, // ← Include.
				UpdatedDate = x.UpdatedDate
			}).ToArrayAsync(token)
		};
	}

	/// <inheritdoc />
	public Task<byte[]?> GetContentsAsync(Guid id, CancellationToken token = default)
	{
		return FindBy(x => x.Id == id)
			.Select(x => x.Contents)
			.FirstOrDefaultAsync(token);
	}

	/// <inheritdoc />
	public Task<Guid[]> GetFileIdsAsync(Guid[] parentIds, CancellationToken token = default)
	{
		return FindBy(x => x.ParentId.HasValue && parentIds.Contains(x.ParentId.Value))
			.Select(x => x.Id)
			.ToArrayAsync(token);
	}

	/// <inheritdoc />
	public Task<string?> GetPropertiesAsync(Guid id, CancellationToken token = default)
	{
		return FindBy(x => x.Id == id)
			.Select(x => x.Properties)
			.FirstOrDefaultAsync(token);
	}

	/// <inheritdoc />
	public Task<int> RemoveAsync(Guid id, CancellationToken token = default)
	{
		return RemoveRangeByAsync(x => x.Id == id, token);
	}

	/// <inheritdoc />
	public Task<int> RemoveRangeByIdsAsync(Guid[] ids, CancellationToken token = default)
	{
		return RemoveRangeByAsync(x => ids.Contains(x.Id), token);
	}

	/// <inheritdoc />
	public Task<int> UpdatePropertiesAsync(
		Guid id,
		Action<UpdateSettersBuilder<FileModel>>[] setters,
		CancellationToken token = default)
	{
		return ExecuteUpdateAsync(x => x.Id == id, setters, token);
	}

	/// <inheritdoc />
	public Task<int> UpdatePropertiesAsync(
		IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]> updates,
		CancellationToken token = default)
	{
		return ExecuteUpdateRangeAsync(updates.Select(ToFilter), token);

		static KeyValuePair<Expression<Func<FileModel, bool>>, Action<UpdateSettersBuilder<FileModel>>[]> ToFilter(
			KeyValuePair<Guid, Action<UpdateSettersBuilder<FileModel>>[]> entry)
		{
			return new(x => x.Id == entry.Key, entry.Value);
		}
	}
	#endregion Methods
}
