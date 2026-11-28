using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Database.Entities.Models;
using DataOrganizer.Database.Repository.Enums;
using DataOrganizer.Database.Repository.Services;
using DataOrganizer.TestSupport.Database;
using DataOrganizer.TestSupport.Models;
using System.Threading.Tasks;

namespace DataOrganizer.Database.UnitTests.Repository.Services;

/// <summary>
/// Tests of <see cref="FileRepository" /> type.
/// </summary>
[TestFixture]
internal sealed class FileRepositoryTests
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
			.Add(EntityFactory.CreateFile(contents: contents, editorState: "props"));

		await database
			.Context
			.SaveChangesAsync();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(database.Context));

		FileRepository sut = mock.Create<FileRepository>();

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
			.Add(EntityFactory.CreateFile(contents: contents, editorState: "props"));

		await database
			.Context
			.SaveChangesAsync();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(database.Context));

		FileRepository sut = mock.Create<FileRepository>();

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
	/// <see cref="FileRepository.GetAllAsync" />: includes the editor state but omits contents when only the editor-state flag is set.
	/// </summary>
	[Test]
	public async Task GetAllAsync_EditorState_Includes_EditorState_Only()
	{
		// Arrange
		using TestDatabase database = new();

		database
			.Context
			.Add(EntityFactory.CreateFile(contents: [1, 2, 3], editorState: "props"));

		await database
			.Context
			.SaveChangesAsync();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(database.Context));

		FileRepository sut = mock.Create<FileRepository>();

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

	/// <summary>
	/// <see cref="FileRepository.GetAllAsync" />: omits both contents and editor state when no flags are set.
	/// </summary>
	[Test]
	public async Task GetAllAsync_None_Excludes_Contents_And_EditorState()
	{
		// Arrange
		using TestDatabase database = new();

		FileEntity stored = EntityFactory.CreateFile(contents: [1, 2, 3], editorState: "props");

		database
			.Context
			.Add(stored);

		await database
			.Context
			.SaveChangesAsync();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(database.Context));

		FileRepository sut = mock.Create<FileRepository>();

		// Act
		FileEntity[] result = await sut.GetAllAsync(OptionalFileProperties.None);

		// Assert
		FileEntity file = result
			.Should()
			.ContainSingle()
			.Which;

		file.Name
			.Should()
			.Be(stored.Name);

		file.Contents
			.Should()
			.BeEmpty();

		file.EditorState
			.Should()
			.BeNull();
	}
	#endregion
}
