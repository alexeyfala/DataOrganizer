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
	public static FileEntity CreateFile(in Guid id = default) => new()
	{
		CreatedAt = DateTime.Now,
		Id = id == default ? Guid.NewGuid() : id,
		Index = RandomValues.CreateIntFrom10To100(),
		Kind = EntityKind.File,
		Name = RandomString.Create(10),
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
	public static FolderEntity CreateFolder(in Guid id = default) => new()
	{
		CreatedAt = DateTime.Now,
		Id = id == default ? Guid.NewGuid() : id,
		Index = RandomValues.CreateIntFrom10To100(),
		Kind = EntityKind.Folder,
		Name = RandomString.Create(10),
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
