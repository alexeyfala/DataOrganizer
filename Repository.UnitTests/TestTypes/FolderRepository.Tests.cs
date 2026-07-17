using AwesomeAssertions;
using Entities.Enums;
using Entities.Models;
using Repository.Services;
using Repository.UnitTests.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(FolderRepository)}"" type")]
internal class FolderRepositoryTests
{
	#region Methods
	/// <summary>
	/// <see cref="FolderRepository.GetFolderSubtreeIdsAsync" />: returns only the requested subtree, excluding unrelated folders.
	/// </summary>
	[Test]
	public async Task GetFolderSubtreeIdsAsync_Excludes_Folders_Outside_The_Subtree()
	{
		// Arrange
		using TestDatabase database = new();

		FolderModel root = CreateFolder(Guid.NewGuid());

		FolderModel child = CreateFolder(Guid.NewGuid(), root.Id);

		FolderModel otherRoot = CreateFolder(Guid.NewGuid());

		FolderModel otherChild = CreateFolder(Guid.NewGuid(), otherRoot.Id);

		database
			.Context
			.AddRange(root, child, otherRoot, otherChild);

		await database
			.Context
			.SaveChangesAsync();

		FolderRepository sut = new(database.Context);

		// Act
		List<Guid> result = await CollectAsync(sut.GetFolderSubtreeIdsAsync(root.Id));

		// Assert
		result
			.Should()
			.BeEquivalentTo([root.Id, child.Id]);
	}

	/// <summary>
	/// <see cref="FolderRepository.GetFolderSubtreeIdsAsync" />: returns only the root when it has no children.
	/// </summary>
	[Test]
	public async Task GetFolderSubtreeIdsAsync_Returns_Only_Root_When_No_Children()
	{
		// Arrange
		using TestDatabase database = new();

		FolderModel root = CreateFolder(Guid.NewGuid());

		database
			.Context
			.Add(root);

		await database
			.Context
			.SaveChangesAsync();

		FolderRepository sut = new(database.Context);

		// Act
		List<Guid> result = await CollectAsync(sut.GetFolderSubtreeIdsAsync(root.Id));

		// Assert
		result
			.Should()
			.Equal(root.Id);
	}

	/// <summary>
	/// <see cref="FolderRepository.GetFolderSubtreeIdsAsync" />: returns the root and every descendant of the subtree.
	/// </summary>
	[Test]
	public async Task GetFolderSubtreeIdsAsync_Returns_Root_And_All_Descendants()
	{
		// Arrange
		using TestDatabase database = new();

		FolderModel root = CreateFolder(Guid.NewGuid());

		FolderModel childA = CreateFolder(Guid.NewGuid(), root.Id);

		FolderModel childB = CreateFolder(Guid.NewGuid(), root.Id);

		FolderModel grandChild = CreateFolder(Guid.NewGuid(), childA.Id);

		database
			.Context
			.AddRange(root, childA, childB, grandChild);

		await database
			.Context
			.SaveChangesAsync();

		FolderRepository sut = new(database.Context);

		// Act
		List<Guid> result = await CollectAsync(sut.GetFolderSubtreeIdsAsync(root.Id));

		// Assert
		result
			.Should()
			.BeEquivalentTo([root.Id, childA.Id, childB.Id, grandChild.Id]);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Materializes an asynchronous sequence into a list.
	/// </summary>
	private static async Task<List<Guid>> CollectAsync(IAsyncEnumerable<Guid> source)
	{
		List<Guid> result = [];

		await foreach (Guid id in source)
		{
			result.Add(id);
		}

		return result;
	}

	/// <summary>
	/// Creates a folder model with the given identifier and optional parent.
	/// </summary>
	private static FolderModel CreateFolder(Guid id, Guid? parentId = null) => new()
	{
		Id = id,
		Index = 0,
		Name = "folder",
		EntityType = EntityType.Folder,
		ParentId = parentId
	};
	#endregion
}
