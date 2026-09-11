using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Services.Hierarchy;
using Entities.Enums;
using Entities.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Common;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TestSupport;

namespace DataOrganizer.UnitTests.Services.Hierarchy;

[TestFixture(Description = $@"Tests of ""{nameof(HierarchyEditor)}"" type")]
internal class HierarchyEditorTests
{
	#region Methods
	/// <summary>
	/// <see cref="HierarchyEditor.AddAsync" />: a new entity is created and, when a parent is given, linked to it and the parent is expanded.
	/// </summary>
	[Test]
	public async Task AddAsync_Returns_Entity(
		[Values] EntityKind type,
		[Values] bool hasParent)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.AddEntityAsync(Arg.Any<AddEntityParameters>())
				.Returns(Substitute.For<ExplorerItemBase>());

			builder.RegisterInstance(dbAccess);

			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Map<ExplorerItemBase, ExplorerItemDtoBase>(Arg.Any<ExplorerItemBase>())
				.Returns(Substitute.For<ExplorerItemDtoBase>());

			builder.RegisterInstance(mapper);
		});

		FolderDto? parent = null;

		if (hasParent)
		{
			parent = new()
			{
				Id = Guid.NewGuid(),
				CreatedAt = default,
				Kind = EntityKind.Folder,
				Index = 0,
				UpdatedAt = default
			};
		}

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [];

		// Act
		ExplorerItemDtoBase? entity = await sut.AddAsync(
			RandomString.Create(10),
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
	[TestCase(EntityKind.Folder)]
	[TestCase(EntityKind.File)]
	public async Task DeleteAsync_Deletes_Entity_In_Database_And_In_Treeview(EntityKind type)
	{
		// Arrange
		ExplorerItemDtoBase toBeDeleted = type switch
		{
			EntityKind.Folder => TestData.CreateFolderDto(),
			EntityKind.File => TestData.CreateFileDto(),
			_ => throw new NotImplementedException()
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			if (type == EntityKind.Folder)
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

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [.. TestData.CreateFolderDtos(5)];

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
	[TestCase(EntityKind.Folder)]
	[TestCase(EntityKind.File)]
	public async Task DeleteAsync_Keeps_The_Entity_When_The_Database_Delete_Fails(EntityKind type)
	{
		// Arrange
		ExplorerItemDtoBase entity = type switch
		{
			EntityKind.Folder => TestData.CreateFolderDto(),
			EntityKind.File => TestData.CreateFileDto(),
			_ => throw new NotImplementedException()
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			if (type == EntityKind.Folder)
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

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [.. TestData.CreateFolderDtos(5)];

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
	/// <see cref="HierarchyEditor.RenameAsync" />: renaming to the same name does nothing and leaves the updated date unchanged.
	/// </summary>
	[Test]
	public async Task RenameAsync_Ignores_An_Unchanged_Name()
	{
		// Arrange
		ExplorerItemDtoBase toBeRenamed = Substitute.For<ExplorerItemDtoBase>();

		string newName = RandomString.Create(10);

		toBeRenamed.Name = newName;

		DateTime updatedAt = DateTime.Now;

		using AutoMock mock = AutoMock.GetLoose();

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		// Act
		bool result = await sut.RenameAsync(toBeRenamed, newName, updatedAt);

		// Assert
		result
			.Should()
			.BeFalse();

		toBeRenamed.UpdatedAt
			.Should()
			.NotBe(updatedAt);
	}

	/// <summary>
	/// <see cref="HierarchyEditor.RenameAsync" />: the dto name and updated date are changed and persisted in the database.
	/// </summary>
	[Test]
	public async Task RenameAsync_Renames_Dto_And_Updates_Name_In_Database_Entity()
	{
		// Arrange
		ExplorerItemDtoBase dto = Substitute.For<ExplorerItemDtoBase>();

		string newName = RandomString.Create(10);

		dto.Name = RandomString.Create(10);

		DateTime updatedAt = DateTime.Now;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess.UpdateFolderPropertiesAsync(
				Arg.Any<Guid>(),
				Arg.Any<Action<UpdateSettersBuilder<FolderEntity>>[]>())
			.Returns(true);

			builder.RegisterInstance(dbAccess);
		});

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		// Act
		bool result = await sut.RenameAsync(dto, newName, updatedAt);

		// Assert
		result
			.Should()
			.BeTrue();

		dto.Name
			.Should()
			.Be(newName);

		dto.UpdatedAt
			.Should()
			.Be(updatedAt);
	}
	#endregion
}
