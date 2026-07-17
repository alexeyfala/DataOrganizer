using AwesomeAssertions;
using Entities.Enums;
using Entities.Models;
using Repository.Enums;
using Repository.Services;
using Repository.UnitTests.Helpers;
using System;
using System.Threading.Tasks;

namespace Repository.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(FileRepository)}"" type")]
internal class FileRepositoryTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileRepository.GetAllAsync" />: includes both contents and properties when both flags are set.
	/// </summary>
	[Test]
	public async Task GetAllAsync_Contents_And_Properties_Includes_Both()
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
		FileModel[] result = await sut.GetAllAsync(OptionalFileProperty.Contents | OptionalFileProperty.Properties);

		// Assert
		FileModel file = result
			.Should()
			.ContainSingle()
			.Which;

		file.Contents
			.Should()
			.Equal(contents);

		file.Properties
			.Should()
			.Be("props");
	}

	/// <summary>
	/// <see cref="FileRepository.GetAllAsync" />: includes contents but omits properties when only the contents flag is set.
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
		FileModel[] result = await sut.GetAllAsync(OptionalFileProperty.Contents);

		// Assert
		FileModel file = result
			.Should()
			.ContainSingle()
			.Which;

		file.Contents
			.Should()
			.Equal(contents);

		file.Properties
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileRepository.GetAllAsync" />: omits both contents and properties when no flags are set.
	/// </summary>
	[Test]
	public async Task GetAllAsync_None_Excludes_Contents_And_Properties()
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
		FileModel[] result = await sut.GetAllAsync(OptionalFileProperty.None);

		// Assert
		FileModel file = result
			.Should()
			.ContainSingle()
			.Which;

		file.Name
			.Should()
			.Be("file");

		file.Contents
			.Should()
			.BeEmpty();

		file.Properties
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FileRepository.GetAllAsync" />: includes properties but omits contents when only the properties flag is set.
	/// </summary>
	[Test]
	public async Task GetAllAsync_Properties_Includes_Properties_Only()
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
		FileModel[] result = await sut.GetAllAsync(OptionalFileProperty.Properties);

		// Assert
		FileModel file = result
			.Should()
			.ContainSingle()
			.Which;

		file.Contents
			.Should()
			.BeEmpty();

		file.Properties
			.Should()
			.Be("props");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a file model with the given contents and properties.
	/// </summary>
	private static FileModel CreateFile(byte[] contents, string? properties) => new()
	{
		Id = Guid.NewGuid(),
		Index = 0,
		Name = "file",
		EntityType = EntityType.File,
		Contents = contents,
		Properties = properties
	};
	#endregion
}
