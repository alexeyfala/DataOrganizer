using Autofac;
using Autofac.Extras.Moq;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Services.Hierarchy;
using Entities.Enums;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using Repository.Interfaces.Database;
using System;
using System.Threading.Tasks;
using TestSupport;

namespace DataOrganizer.UnitTests.Services.Hierarchy;

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
				Arg.Any<Action<UpdateSettersBuilder<FolderEntity>>[]>());
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
		await sut.UpdateIsFavoriteAsync(TestData.CreateFileDto());

		// Assert
		await dbAccess
			.Received()
			.UpdateFilePropertiesAsync(
				Arg.Any<Guid>(),
				Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>());
	}

	/// <summary>
	/// <see cref="EntityPropertyWriter.UpdateIsSelectedAsync" />: the IsSelected property of a file or dataset is persisted via the file update.
	/// </summary>
	[Test]
	public async Task UpdateIsSelectedAsync_Persists_File_In_Database([Values(EntityKind.File, EntityKind.Dataset)] EntityKind kind)
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dbAccess));

		EntityPropertyWriter sut = mock.Create<EntityPropertyWriter>();

		FileDto dto = new()
		{
			CreatedAt = default,
			Id = Guid.NewGuid(),
			Index = 0,
			Kind = kind,
			UpdatedAt = default
		};

		// Act
		await sut.UpdateIsSelectedAsync(dto);

		// Assert
		await dbAccess
			.Received()
			.UpdateFilePropertiesAsync(
				Arg.Any<Guid>(),
				Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>());
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
		await sut.UpdateIsSelectedAsync(TestData.CreateFolderDto());

		// Assert
		await dbAccess
			.Received()
			.UpdateFolderPropertiesAsync(
				Arg.Any<Guid>(),
				Arg.Any<Action<UpdateSettersBuilder<FolderEntity>>[]>());
	}
	#endregion
}
