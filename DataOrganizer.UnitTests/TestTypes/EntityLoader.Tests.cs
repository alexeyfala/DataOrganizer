using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities.Models;
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
	#endregion
}
