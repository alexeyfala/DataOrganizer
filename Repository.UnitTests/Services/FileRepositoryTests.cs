using AwesomeAssertions;
using Entities.Enums;
using Entities.Models;
using Repository.Enums;
using Repository.Services;
using Repository.UnitTests.Fixtures;
using System;
using System.Threading.Tasks;

namespace Repository.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(FileRepository)}"" type")]
internal class FileRepositoryTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileRepository.GetAllAsync" />: includes both contents and editor state when both flags are set.
	/// </summary>
	[Test]
	public async Task GetAllAsync_Contents_And_EditorState_Includes_Both()
	{
		// Arrange
		byte[] contents = [1, 2, 3];

		using TestDatabase database = new();

		database
			.Context
			.Add(CreateFile(contents, "props"));

		await database
			.Context
			.SaveChangesAsync();

		FileRepository sut = new(database.Context);

		// Act
		FileEntity[] result = await sut.GetAllAsync(OptionalFileProperties.Contents | OptionalFileProperties.EditorState);

		// Assert
		FileEntity file = result
			.Should()
			.ContainSingle()
			.Which;

		file.Contents
			.Should()
			.Equal(contents);

		file.EditorState
			.Should()
			.Be("props");
	}

	/// <summary>
	/// <see cref="FileRepository.GetAllAsync" />: includes contents but omits the editor state when only the contents flag is set.
	/// </summary>
	[Test]
	public async Task GetAllAsync_Contents_Includes_Contents_Only()
	{
		// Arrange
		byte[] contents = [1, 2, 3];

		using TestDatabase database = new();

		database
			.Context
			.Add(CreateFile(contents, "props"));

		await database
			.Context
			.SaveChangesAsync();

		FileRepository sut = new(database.Context);

		// Act
		FileEntity[] result = await sut.GetAllAsync(OptionalFileProperties.Contents);

		// Assert
		FileEntity file = result
			.Should()
			.ContainSingle()
			.Which;

		file.Contents
			.Should()
			.Equal(contents);

		file.EditorState
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileRepository.GetAllAsync" />: omits both contents and editor state when no flags are set.
	/// </summary>
	[Test]
	public async Task GetAllAsync_None_Excludes_Contents_And_EditorState()
	{
		// Arrange
		using TestDatabase database = new();

		database
			.Context
			.Add(CreateFile([1, 2, 3], "props"));

		await database
			.Context
			.SaveChangesAsync();

		FileRepository sut = new(database.Context);

		// Act
		FileEntity[] result = await sut.GetAllAsync(OptionalFileProperties.None);

		// Assert
		FileEntity file = result
			.Should()
			.ContainSingle()
			.Which;

		file.Name
			.Should()
			.Be("file");

		file.Contents
			.Should()
			.BeEmpty();

		file.EditorState
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileRepository.GetAllAsync" />: includes the editor state but omits contents when only the editor-state flag is set.
	/// </summary>
	[Test]
	public async Task GetAllAsync_EditorState_Includes_EditorState_Only()
	{
		// Arrange
		using TestDatabase database = new();

		database
			.Context
			.Add(CreateFile([1, 2, 3], "props"));

		await database
			.Context
			.SaveChangesAsync();

		FileRepository sut = new(database.Context);

		// Act
		FileEntity[] result = await sut.GetAllAsync(OptionalFileProperties.EditorState);

		// Assert
		FileEntity file = result
			.Should()
			.ContainSingle()
			.Which;

		file.Contents
			.Should()
			.BeEmpty();

		file.EditorState
			.Should()
			.Be("props");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a file model with the given contents and editor state.
	/// </summary>
	private static FileEntity CreateFile(byte[] contents, string? editorState) => new()
	{
		Id = Guid.NewGuid(),
		Index = 0,
		Name = "file",
		Kind = EntityKind.File,
		Contents = contents,
		EditorState = editorState
	};
	#endregion
}
