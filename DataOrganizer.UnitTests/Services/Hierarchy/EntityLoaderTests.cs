using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Services.Hierarchy;
using Entities.Models;
using Mapster;
using MapsterMapper;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Enums;
using Repository.Interfaces.Database;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestSupport;

namespace DataOrganizer.UnitTests.Services.Hierarchy;

[TestFixture(Description = $@"Tests of ""{nameof(EntityLoader)}"" type")]
internal class EntityLoaderTests
{
	#region Methods
	/// <summary>
	/// <see cref="EntityLoader.LoadHierarchyAsync" />: a cancelled load is the caller giving up,
	/// so it leaves as a cancellation.
	/// </summary>
	[Test]
	public async Task LoadHierarchyAsync_Passes_A_Cancellation_On()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetAllFoldersAsync(Arg.Any<CancellationToken>())
				.ThrowsAsync(new OperationCanceledException());

			builder.RegisterInstance(CreateMapper());

			builder.RegisterInstance(dbAccess);
		});

		EntityLoader sut = mock.Create<EntityLoader>();

		// Act
		Func<Task> act = () => sut.LoadHierarchyAsync();

		// Assert
		await act
			.Should()
			.ThrowAsync<OperationCanceledException>();
	}

	/// <summary>
	/// <see cref="EntityLoader.LoadHierarchyAsync" />: a database that cannot be read is reported
	/// as such, not as a database without objects.
	/// </summary>
	[Test]
	public async Task LoadHierarchyAsync_Reports_A_Database_It_Cannot_Read()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetAllFoldersAsync()
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(CreateMapper());

			builder.RegisterInstance(dbAccess);
		});

		EntityLoader sut = mock.Create<EntityLoader>();

		// Act
		ExplorerItemDtoBase[]? hierarchy = await sut.LoadHierarchyAsync();

		// Assert
		hierarchy
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EntityLoader.LoadHierarchyAsync" />: loads folders and files and returns a hierarchy containing all of them.
	/// </summary>
	[Test]
	public async Task LoadHierarchyAsync_Returns_All_Folders_And_Files()
	{
		// Arrange
		const int folderCount = 5;

		const int fileCount = 5;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetAllFoldersAsync()
				.Returns([.. TestData.CreateFolders(folderCount)]);

			dbAccess
				.GetAllFilesAsync(OptionalFileProperties.None)
				.Returns([.. TestData.CreateFiles(fileCount)]);

			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Config
				.Returns(Substitute.For<TypeAdapterConfig>());

			mapper
				.Map<IEnumerable<FileEntity>, FileDto[]>(Arg.Any<IEnumerable<FileEntity>>())
				.Returns([.. TestData.CreateFileDtos(fileCount)]);

			mapper
				.Map<IEnumerable<FolderEntity>, FolderDto[]>(Arg.Any<IEnumerable<FolderEntity>>())
				.Returns([.. TestData.CreateFolderDtos(folderCount)]);

			builder.RegisterInstance(mapper);

			builder.RegisterInstance(dbAccess);
		});

		EntityLoader sut = mock.Create<EntityLoader>();

		// Act
		ExplorerItemDtoBase[]? hierarchy = await sut.LoadHierarchyAsync();

		// Assert
		hierarchy?.Length
			.Should()
			.Be(folderCount + fileCount);
	}

	/// <summary>
	/// <see cref="EntityLoader.Map" />: a folder holding a wrapped key and everything under it are marked as encrypted.
	/// </summary>
	[Test]
	public void Map_Marks_The_Subtree_Of_A_Password_Keeper_As_Encrypted()
	{
		// Arrange
		FolderDto keeper = TestData.CreateFolderDto();

		keeper.EncryptedDek = TestData.CreateRandomBytes(10);

		FolderDto plainFolder = TestData.CreateFolderDto();

		FileDto keptFile = TestData.CreateFileDto();

		keptFile.ParentId = keeper.Id;

		FileDto plainFile = TestData.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Config
				.Returns(Substitute.For<TypeAdapterConfig>());

			mapper
				.Map<IEnumerable<FileEntity>, FileDto[]>(Arg.Any<IEnumerable<FileEntity>>())
				.Returns([keptFile, plainFile]);

			mapper
				.Map<IEnumerable<FolderEntity>, FolderDto[]>(Arg.Any<IEnumerable<FolderEntity>>())
				.Returns([keeper, plainFolder]);

			builder.RegisterInstance(mapper);
		});

		EntityLoader sut = mock.Create<EntityLoader>();

		// Act
		sut.Map([], []);

		// Assert
		keeper.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Encrypted);

		keptFile.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Encrypted);

		plainFolder.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.None);

		plainFile.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.None);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// A mapper the constructor of the loader can configure.
	/// </summary>
	private static IMapper CreateMapper()
	{
		IMapper mapper = Substitute.For<IMapper>();

		mapper
			.Config
			.Returns(Substitute.For<TypeAdapterConfig>());

		return mapper;
	}
	#endregion
}
