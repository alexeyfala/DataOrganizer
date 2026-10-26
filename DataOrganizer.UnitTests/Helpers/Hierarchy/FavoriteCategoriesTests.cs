using AwesomeAssertions;
using DataOrganizer.Dto.Favorites;
using DataOrganizer.Helpers.Hierarchy;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Hierarchy.UnitTests.Factories;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.UnitTests.Helpers.Hierarchy;

[TestFixture(Description = $@"Tests of ""{nameof(FavoriteCategories)}"" type")]
internal class FavoriteCategoriesTests
{
	#region Methods
	/// <summary>
	/// <see cref="FavoriteCategories.Gather" />: the favorite files gather by their folder, the top-level ones into a root
	/// category, numbered in the order of the tree walk, and a folder without favorites gives no category.
	/// </summary>
	[Test]
	public void Gather_Gathers_The_Favorites_By_Their_Folder()
	{
		// Arrange
		FileDto rootFavorite = ItemDtoFactory.CreateFileDto();

		FolderDto outer = ItemDtoFactory.CreateFolderDto(encryptionStatus: EncryptionStatus.Decrypted);

		FileDto outerFavorite = ItemDtoFactory.CreateFileDto();

		FolderDto inner = ItemDtoFactory.CreateFolderDto();

		FileDto innerFavorite = ItemDtoFactory.CreateFileDto();

		FolderDto empty = ItemDtoFactory.CreateFolderDto();

		rootFavorite.IsFavorite = outerFavorite.IsFavorite = innerFavorite.IsFavorite = true;

		outerFavorite.Parent = inner.Parent = outer;

		innerFavorite.Parent = inner;

		outer.Children.AddRange([inner, outerFavorite]);

		inner.Children.Add(innerFavorite);

		empty.Children.Add(ItemDtoFactory.CreateFileDto());

		// Act
		List<FavoriteCategory> result = FavoriteCategories.Gather([empty, outer, rootFavorite]);

		// Assert
		result
			.Should()
			.SatisfyRespectively(
				x => (x.Id, x.Name, x.EncryptionStatus, x.Index, x.Children.Single())
					.Should()
					.Be((Guid.Parse("210B84EF-06EA-4B70-97E8-DC4BE4DD6195"), "Root", EncryptionStatus.None, 0, rootFavorite)),
				x => (x.Id, x.Name, x.EncryptionStatus, x.Index, x.Children.Single())
					.Should()
					.Be((outer.Id, outer.Name, EncryptionStatus.Decrypted, 1, outerFavorite)),
				x => (x.Id, x.Name, x.EncryptionStatus, x.Index, x.Children.Single())
					.Should()
					.Be((inner.Id, inner.Name, EncryptionStatus.None, 2, innerFavorite)));
	}

	/// <summary>
	/// <see cref="FavoriteCategories.Gather" />: the favorites of one folder stay together in their order, without the
	/// files that are not favorite.
	/// </summary>
	[Test]
	public void Gather_Keeps_The_Favorites_Of_A_Folder_Together()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(3)];

		files[0].IsFavorite = files[2].IsFavorite = true;

		// Act
		List<FavoriteCategory> result = FavoriteCategories.Gather(files);

		// Assert
		result
			.Should()
			.ContainSingle();

		result[0].Children
			.Should()
			.Equal(files[0], files[2]);
	}

	/// <summary>
	/// <see cref="FavoriteCategories.Gather" />: a tree without favorites gives no category.
	/// </summary>
	[Test]
	public void Gather_Without_Favorites_Is_Empty()
	{
		// Act
		List<FavoriteCategory> result = FavoriteCategories.Gather([.. ItemDtoFactory.CreateFileDtos(2)]);

		// Assert
		result
			.Should()
			.BeEmpty();
	}
	#endregion
}
