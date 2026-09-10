using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
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

public sealed class FileRepository : RepositoryBase<FileEntity>, IFileRepository
{
	#region Constructors
	public FileRepository(SqliteDbContext context) : base(context)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task<FileEntity[]> GetAllAsync(OptionalFileProperties optionalProperties, CancellationToken token = default)
	{
		bool includeContents = optionalProperties.HasFlag(OptionalFileProperties.Contents);

		bool includeEditorState = optionalProperties.HasFlag(OptionalFileProperties.EditorState);

		return (includeContents, includeEditorState) switch
		{
			(false, false) => FindAll().Select(x => new FileEntity
			{
				CreatedAt = x.CreatedAt,
				Hotkeys = x.Hotkeys,
				Id = x.Id,
				Index = x.Index,
				IsFavorite = x.IsFavorite,
				IsSelected = x.IsSelected,
				Kind = x.Kind,
				Name = x.Name,
				Note = x.Note,
				ParentId = x.ParentId,
				UpdatedAt = x.UpdatedAt
			}).ToArrayAsync(token),
			(true, false) => FindAll().Select(x => new FileEntity
			{
				Contents = x.Contents, // ← Include.
				CreatedAt = x.CreatedAt,
				Hotkeys = x.Hotkeys,
				Id = x.Id,
				Index = x.Index,
				IsFavorite = x.IsFavorite,
				IsSelected = x.IsSelected,
				Kind = x.Kind,
				Name = x.Name,
				Note = x.Note,
				ParentId = x.ParentId,
				UpdatedAt = x.UpdatedAt
			}).ToArrayAsync(token),
			(false, true) => FindAll().Select(x => new FileEntity
			{
				CreatedAt = x.CreatedAt,
				EditorState = x.EditorState, // ← Include.
				Hotkeys = x.Hotkeys,
				Id = x.Id,
				Index = x.Index,
				IsFavorite = x.IsFavorite,
				IsSelected = x.IsSelected,
				Kind = x.Kind,
				Name = x.Name,
				Note = x.Note,
				ParentId = x.ParentId,
				UpdatedAt = x.UpdatedAt
			}).ToArrayAsync(token),
			(true, true) => FindAll().Select(x => new FileEntity
			{
				Contents = x.Contents, // ← Include.
				CreatedAt = x.CreatedAt,
				EditorState = x.EditorState, // ← Include.
				Hotkeys = x.Hotkeys,
				Id = x.Id,
				Index = x.Index,
				IsFavorite = x.IsFavorite,
				IsSelected = x.IsSelected,
				Kind = x.Kind,
				Name = x.Name,
				Note = x.Note,
				ParentId = x.ParentId,
				UpdatedAt = x.UpdatedAt
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
	public Task<string?> GetEditorStateAsync(Guid id, CancellationToken token = default)
	{
		return FindBy(x => x.Id == id)
			.Select(x => x.EditorState)
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
		Action<UpdateSettersBuilder<FileEntity>>[] setters,
		CancellationToken token = default)
	{
		return ExecuteUpdateAsync(x => x.Id == id, setters, token);
	}

	/// <inheritdoc />
	public Task<int> UpdatePropertiesAsync(
		IDictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]> updates,
		CancellationToken token = default)
	{
		return ExecuteUpdateRangeAsync(updates.Select(ToFilter), token);

		static KeyValuePair<Expression<Func<FileEntity, bool>>, Action<UpdateSettersBuilder<FileEntity>>[]> ToFilter(
			KeyValuePair<Guid, Action<UpdateSettersBuilder<FileEntity>>[]> entry)
		{
			return new(x => x.Id == entry.Key, entry.Value);
		}
	}
	#endregion
}
