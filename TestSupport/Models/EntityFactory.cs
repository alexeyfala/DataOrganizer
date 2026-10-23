using Entities.Enums;
using Entities.Models;
using Shared.Common;
using System;
using System.Collections.Generic;
using TestSupport.Common;

namespace TestSupport.Models;

/// <summary>
/// Factory methods that build stored entities filled with random values.
/// </summary>
public static class EntityFactory
{
	#region Methods
	/// <summary>
	/// Creates a <see cref="FileEntity" /> with random properties.
	/// </summary>
	public static FileEntity CreateFile(
		in Guid id = default,
		byte[]? contents = null,
		string? editorState = null,
		in Guid? parentId = null,
		bool isFavorite = false,
		bool isSelected = false,
		byte[]? note = null) => new()
		{
			Contents = contents ?? [],
			CreatedAt = DateTime.Now,
			EditorState = editorState,
			Id = id == default ? Guid.NewGuid() : id,
			Index = RandomValues.CreateIntFrom10To100(),
			IsFavorite = isFavorite,
			IsSelected = isSelected,
			Kind = EntityKind.File,
			Name = RandomString.Create(10),
			Note = note,
			ParentId = parentId,
			UpdatedAt = DateTime.Now
		};

	/// <summary>
	/// Creates the required number of random <see cref="FileEntity" /> objects.
	/// </summary>
	public static IEnumerable<FileEntity> CreateFiles(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return CreateFile();
		}
	}

	/// <summary>
	/// Creates a <see cref="FolderEntity" /> with random properties.
	/// </summary>
	public static FolderEntity CreateFolder(
		in Guid id = default,
		in Guid? parentId = null,
		byte[]? encryptedDek = null,
		bool isExpanded = false,
		bool isSelected = false,
		byte[]? note = null) => new()
		{
			CreatedAt = DateTime.Now,
			EncryptedDek = encryptedDek,
			Id = id == default ? Guid.NewGuid() : id,
			Index = RandomValues.CreateIntFrom10To100(),
			IsExpanded = isExpanded,
			IsSelected = isSelected,
			Kind = EntityKind.Folder,
			Name = RandomString.Create(10),
			Note = note,
			ParentId = parentId,
			UpdatedAt = DateTime.Now
		};

	/// <summary>
	/// Creates the required number of random <see cref="FolderEntity" /> objects.
	/// </summary>
	public static IEnumerable<FolderEntity> CreateFolders(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return CreateFolder();
		}
	}
	#endregion
}
