using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Services;
using Entities.Models;
using Mapster;
using MapsterMapper;
using NSubstitute;
using Repository.Enums;
using Repository.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EntityLoader)}"" type")]
internal class EntityLoaderTests
{
	#region Methods
	/// <summary>
	/// <see cref="EntityLoader.LoadFromEmbeddedDbAsync" />: loads folders and files and returns a hierarchy containing all of them.
	/// </summary>
	[Test]
	public async Task LoadFromEmbeddedDbAsync_Does_Work()
	{
		// Arrange
		const int folderCount = 5;

		const int fileCount = 5;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetAllFoldersAsync()
				.Returns([.. TestUtils.CreateFolders(folderCount)]);

			dbAccess
				.GetAllFilesAsync(OptionalFileProperty.None)
				.Returns([.. TestUtils.CreateFiles(fileCount)]);

			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Config
				.Returns(Substitute.For<TypeAdapterConfig>());

			mapper
				.Map<IEnumerable<FileModel>, FileModelDto[]>(Arg.Any<IEnumerable<FileModel>>())
				.Returns([.. TestUtils.CreateFilesDto(fileCount)]);

			mapper
				.Map<IEnumerable<FolderModel>, FolderModelDto[]>(Arg.Any<IEnumerable<FolderModel>>())
				.Returns([.. TestUtils.CreateFoldersDto(folderCount)]);

			builder.RegisterInstance(mapper);

			builder.RegisterInstance(dbAccess);
		});

		EntityLoader sut = mock.Create<EntityLoader>();

		// Act
		ExplorerModelBaseDto[] hierarchy = await sut.LoadFromEmbeddedDbAsync();

		// Assert
		hierarchy.Length
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
		FolderModelDto keeper = TestUtils.CreateFolderDto();

		keeper.EncryptedDek = TestUtils.CreateRandomBytes(10);

		FolderModelDto plainFolder = TestUtils.CreateFolderDto();

		FileModelDto keptFile = TestUtils.CreateFileDto();

		keptFile.ParentId = keeper.Id;

		FileModelDto plainFile = TestUtils.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Config
				.Returns(Substitute.For<TypeAdapterConfig>());

			mapper
				.Map<IEnumerable<FileModel>, FileModelDto[]>(Arg.Any<IEnumerable<FileModel>>())
				.Returns([keptFile, plainFile]);

			mapper
				.Map<IEnumerable<FolderModel>, FolderModelDto[]>(Arg.Any<IEnumerable<FolderModel>>())
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
}
