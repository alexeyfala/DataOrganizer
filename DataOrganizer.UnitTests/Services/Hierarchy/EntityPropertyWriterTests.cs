using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Services.Hierarchy;
using Entities.Enums;
using Entities.Models;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using Repository.Enums;
using Repository.Interfaces;
using Repository.Interfaces.Database;
using Repository.Services;
using Repository.Services.Database;
using System.Threading.Tasks;
using TestSupport.Database;
using TestSupport.Models;

namespace DataOrganizer.UnitTests.Services.Hierarchy;

[TestFixture(Description = $@"Tests of ""{nameof(EntityPropertyWriter)}"" type")]
internal class EntityPropertyWriterTests
{
	#region Methods
	/// <summary>
	/// <see cref="EntityPropertyWriter.UpdateIsExpandedAsync" />: the expansion of the folder is written to the database.
	/// </summary>
	[Test]
	public async Task UpdateIsExpandedAsync_Persists_In_Database([Values] bool isExpanded)
	{
		// Arrange
		using TestDatabase database = new();

		FolderEntity folder = EntityFactory.CreateFolder(isExpanded: !isExpanded);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FolderRepository>()
				.As<IFolderRepository>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFoldersAsync([folder]);

		EntityPropertyWriter sut = mock.Create<EntityPropertyWriter>();

		// Act
		await sut.UpdateIsExpandedAsync(folder.Id, isExpanded);

		// Assert
		FolderEntity[] stored = await dbAccess.GetAllFoldersAsync();

		stored
			.Should()
			.ContainSingle()
			.Which
			.IsExpanded
			.Should()
			.Be(isExpanded);
	}

	/// <summary>
	/// <see cref="EntityPropertyWriter.UpdateIsFavoriteAsync" />: the favorite mark of the file is written to the database.
	/// </summary>
	[Test]
	public async Task UpdateIsFavoriteAsync_Persists_In_Database([Values] bool isFavorite)
	{
		// Arrange
		using TestDatabase database = new();

		FileEntity file = EntityFactory.CreateFile(isFavorite: !isFavorite);

		FileDto dto = ItemDtoFactory.CreateFileDto(id: file.Id);

		dto.IsFavorite = isFavorite;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFilesAsync([file]);

		EntityPropertyWriter sut = mock.Create<EntityPropertyWriter>();

		// Act
		await sut.UpdateIsFavoriteAsync(dto);

		// Assert
		FileEntity[] stored = await dbAccess.GetAllFilesAsync(OptionalFileProperties.None);

		stored
			.Should()
			.ContainSingle()
			.Which
			.IsFavorite
			.Should()
			.Be(isFavorite);
	}

	/// <summary>
	/// <see cref="EntityPropertyWriter.UpdateIsSelectedAsync" />: the selection of a file or a dataset is written to its file
	/// in the database.
	/// </summary>
	[Test]
	public async Task UpdateIsSelectedAsync_Persists_File_In_Database(
		[Values(EntityKind.File, EntityKind.Dataset)] EntityKind kind,
		[Values] bool isSelected)
	{
		// Arrange
		using TestDatabase database = new();

		FileEntity file = EntityFactory.CreateFile(isSelected: !isSelected);

		FileDto dto = ItemDtoFactory.CreateFileDto(id: file.Id, kind: kind);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFilesAsync([file]);

		EntityPropertyWriter sut = mock.Create<EntityPropertyWriter>();

		// Act
		await sut.UpdateIsSelectedAsync(dto, isSelected);

		// Assert
		FileEntity[] stored = await dbAccess.GetAllFilesAsync(OptionalFileProperties.None);

		stored
			.Should()
			.ContainSingle()
			.Which
			.IsSelected
			.Should()
			.Be(isSelected);
	}

	/// <summary>
	/// <see cref="EntityPropertyWriter.UpdateIsSelectedAsync" />: the selection of a folder is written to the folder in the
	/// database.
	/// </summary>
	[Test]
	public async Task UpdateIsSelectedAsync_Persists_Folder_In_Database([Values] bool isSelected)
	{
		// Arrange
		using TestDatabase database = new();

		FolderEntity folder = EntityFactory.CreateFolder(isSelected: !isSelected);

		FolderDto dto = ItemDtoFactory.CreateFolderDto(id: folder.Id);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FolderRepository>()
				.As<IFolderRepository>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFoldersAsync([folder]);

		EntityPropertyWriter sut = mock.Create<EntityPropertyWriter>();

		// Act
		await sut.UpdateIsSelectedAsync(dto, isSelected);

		// Assert
		FolderEntity[] stored = await dbAccess.GetAllFoldersAsync();

		stored
			.Should()
			.ContainSingle()
			.Which
			.IsSelected
			.Should()
			.Be(isSelected);
	}
	#endregion
}
