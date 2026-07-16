using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Services;
using Entities.Enums;
using Entities.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using Repository.DTO;
using Repository.Interfaces;
using Shared.Common;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(HierarchyEditor)}"" type")]
internal class HierarchyEditorTests
{
	#region Methods
	/// <summary>
	/// <see cref="HierarchyEditor.AddAsync" />: a new entity is created and, when a parent is given, linked to it and the parent is expanded.
	/// </summary>
	[Test]
	public async Task AddAsync_Returns_Entity(
		[Values] EntityType type,
		[Values] bool hasParent)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.AddEntityAsync(Arg.Any<AddEntityParameters>())
				.Returns(Substitute.For<ExplorerModelBase>());

			builder.RegisterInstance(dbAccess);

			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Map<ExplorerModelBase, ExplorerModelBaseDto>(Arg.Any<ExplorerModelBase>())
				.Returns(Substitute.For<ExplorerModelBaseDto>());

			builder.RegisterInstance(mapper);
		});

		FolderModelDto? parent = null;

		if (hasParent)
		{
			parent = new()
			{
				Id = Guid.NewGuid(),
				CreatedDate = default,
				EntityType = EntityType.Folder,
				Index = 0,
				UpdatedDate = default
			};
		}

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		ObservableCollection<ExplorerModelBaseDto> hierarchy = [];

		// Act
		ExplorerModelBaseDto? entity = await sut.AddAsync(
			AppUtils.CreateRandomString(10),
			type,
			parent,
			hierarchy);

		// Assert
		entity
			.Should()
			.NotBeNull();

		if (parent is null)
		{
			hierarchy
				.Should()
				.Contain(entity);

			return;
		}

		entity.Parent
			.Should()
			.BeSameAs(parent);

		parent.Children
			.Should()
			.Contain(entity);

		parent.IsExpanded
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="HierarchyEditor.DeleteAsync" />: on success the entity is removed from the hierarchy.
	/// </summary>
	[TestCase(EntityType.Folder)]
	[TestCase(EntityType.File)]
	public async Task DeleteAsync_Deletes_Entity_In_Database_And_In_Treeview(EntityType type)
	{
		// Arrange
		ExplorerModelBaseDto toBeDeleted = type switch
		{
			EntityType.Folder => TestUtils.CreateFolderDto(),
			EntityType.File => TestUtils.CreateFileDto(),
			_ => throw new NotImplementedException()
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			if (type == EntityType.Folder)
			{
				dbAccess
					.DeleteFolderAsync(toBeDeleted.Id)
					.Returns(true);
			}
			else
			{
				dbAccess
					.DeleteFileAsync(toBeDeleted.Id)
					.Returns(true);
			}

			builder.RegisterInstance(dbAccess);
		});

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		ObservableCollection<ExplorerModelBaseDto> hierarchy = [.. TestUtils.CreateFoldersDto(5)];

		hierarchy.Add(toBeDeleted);

		// Act
		bool result = await sut.DeleteAsync(toBeDeleted, hierarchy);

		// Assert
		result
			.Should()
			.BeTrue();

		hierarchy
			.Should()
			.NotContain(toBeDeleted);
	}

	/// <summary>
	/// <see cref="HierarchyEditor.DeleteAsync" />: when the database delete fails the entity stays in the hierarchy.
	/// </summary>
	[TestCase(EntityType.Folder)]
	[TestCase(EntityType.File)]
	public async Task DeleteAsync_Should_Not_Delete_Entity_In_Database_And_In_Treeview(EntityType type)
	{
		// Arrange
		ExplorerModelBaseDto entity = type switch
		{
			EntityType.Folder => TestUtils.CreateFolderDto(),
			EntityType.File => TestUtils.CreateFileDto(),
			_ => throw new NotImplementedException()
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			if (type == EntityType.Folder)
			{
				dbAccess
					.DeleteFolderAsync(entity.Id)
					.Returns(false);
			}
			else
			{
				dbAccess
					.DeleteFileAsync(entity.Id)
					.Returns(false);
			}

			builder.RegisterInstance(dbAccess);
		});

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		ObservableCollection<ExplorerModelBaseDto> hierarchy = [.. TestUtils.CreateFoldersDto(5)];

		hierarchy.Add(entity);

		// Act
		bool result = await sut.DeleteAsync(entity, hierarchy);

		// Assert
		result
			.Should()
			.BeFalse();

		hierarchy
			.Should()
			.Contain(entity);
	}

	/// <summary>
	/// <see cref="HierarchyEditor.RenameAsync" />: the dto name and updated date are changed and persisted in the database.
	/// </summary>
	[Test]
	public async Task RenameAsync_Renames_Dto_And_Updates_Name_In_Database_Entity()
	{
		// Arrange
		ExplorerModelBaseDto dto = Substitute.For<ExplorerModelBaseDto>();

		string newName = AppUtils.CreateRandomString(10);

		dto.Name = AppUtils.CreateRandomString(10);

		DateTime updatedDate = DateTime.Now;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess.UpdateFolderPropertiesAsync(
				Arg.Any<Guid>(),
				Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>())
			.Returns(true);

			builder.RegisterInstance(dbAccess);
		});

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		// Act
		bool result = await sut.RenameAsync(dto, newName, updatedDate);

		// Assert
		result
			.Should()
			.BeTrue();

		dto.Name
			.Should()
			.Be(newName);

		dto.UpdatedDate
			.Should()
			.Be(updatedDate);
	}

	/// <summary>
	/// <see cref="HierarchyEditor.RenameAsync" />: renaming to the same name does nothing and leaves the updated date unchanged.
	/// </summary>
	[Test]
	public async Task RenameAsync_Should_Do_Nothing_If_Name_Is_The_Same()
	{
		// Arrange
		ExplorerModelBaseDto toBeRenamed = Substitute.For<ExplorerModelBaseDto>();

		string newName = AppUtils.CreateRandomString(10);

		toBeRenamed.Name = newName;

		DateTime updatedDate = DateTime.Now;

		using AutoMock mock = AutoMock.GetLoose();

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		// Act
		bool result = await sut.RenameAsync(toBeRenamed, newName, updatedDate);

		// Assert
		result
			.Should()
			.BeFalse();

		toBeRenamed.UpdatedDate
			.Should()
			.NotBe(updatedDate);
	}
	#endregion
}
