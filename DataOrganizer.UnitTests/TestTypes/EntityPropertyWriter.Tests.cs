using Autofac;
using Autofac.Extras.Moq;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Services;
using Entities.Enums;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using Repository.Interfaces;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EntityPropertyWriter)}"" type")]
internal class EntityPropertyWriterTests
{
	#region Methods
	/// <summary>
	/// <see cref="EntityPropertyWriter.UpdateIsExpandedAsync" />: the folder IsExpanded property is persisted in the database.
	/// </summary>
	[Test]
	public async Task UpdateIsExpandedAsync_Persists_In_Database([Values] bool isExpanded)
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dbAccess));

		EntityPropertyWriter sut = mock.Create<EntityPropertyWriter>();

		// Act
		await sut.UpdateIsExpandedAsync(Guid.NewGuid(), isExpanded);

		// Assert
		await dbAccess
			.Received()
			.UpdateFolderPropertiesAsync(
				Arg.Any<Guid>(),
				Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>());
	}

	/// <summary>
	/// <see cref="EntityPropertyWriter.UpdateIsFavoriteAsync" />: the file IsFavorite property is persisted in the database.
	/// </summary>
	[Test]
	public async Task UpdateIsFavoriteAsync_Persists_In_Database()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dbAccess));

		EntityPropertyWriter sut = mock.Create<EntityPropertyWriter>();

		// Act
		await sut.UpdateIsFavoriteAsync(TestUtils.CreateFileDto());

		// Assert
		await dbAccess
			.Received()
			.UpdateFilePropertiesAsync(
				Arg.Any<Guid>(),
				Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>());
	}

	/// <summary>
	/// <see cref="EntityPropertyWriter.UpdateIsSelectedAsync" />: the IsSelected property of a file or dataset is persisted via the file update.
	/// </summary>
	[Test]
	public async Task UpdateIsSelectedAsync_Persists_File_In_Database([Values(EntityType.File, EntityType.DataSet)] EntityType entityType)
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dbAccess));

		EntityPropertyWriter sut = mock.Create<EntityPropertyWriter>();

		FileModelDto dto = new()
		{
			CreatedDate = default,
			EntityType = entityType,
			Id = Guid.NewGuid(),
			Index = 0,
			UpdatedDate = default
		};

		// Act
		await sut.UpdateIsSelectedAsync(dto);

		// Assert
		await dbAccess
			.Received()
			.UpdateFilePropertiesAsync(
				Arg.Any<Guid>(),
				Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>());
	}

	/// <summary>
	/// <see cref="EntityPropertyWriter.UpdateIsSelectedAsync" />: the folder IsSelected property is persisted via the folder update.
	/// </summary>
	[Test]
	public async Task UpdateIsSelectedAsync_Persists_Folder_In_Database()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dbAccess));

		EntityPropertyWriter sut = mock.Create<EntityPropertyWriter>();

		// Act
		await sut.UpdateIsSelectedAsync(TestUtils.CreateFolderDto());

		// Assert
		await dbAccess
			.Received()
			.UpdateFolderPropertiesAsync(
				Arg.Any<Guid>(),
				Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>());
	}
	#endregion
}
