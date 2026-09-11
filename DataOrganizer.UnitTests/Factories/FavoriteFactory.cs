using DataOrganizer.Dto.Favorites;
using DataOrganizer.Enums.Encryption;
using Shared.Common;
using System;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.Factories;

/// <summary>
/// Factory methods that build favorites transfer objects filled with random values.
/// </summary>
public static class FavoriteFactory
{
	#region Methods
	/// <summary>
	/// Creates the required number of random <see cref="FavoriteCategory" /> objects.
	/// </summary>
	public static IEnumerable<FavoriteCategory> CreateFavoriteCategories(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return CreateFavoriteCategory();
		}
	}

	/// <summary>
	/// Creates a <see cref="FavoriteCategory" /> with random properties.
	/// </summary>
	public static FavoriteCategory CreateFavoriteCategory() => new()
	{
		Children = [],
		EncryptionStatus = EncryptionStatus.None,
		Id = Guid.NewGuid(),
		Index = default,
		Name = RandomString.Create(10)
	};

	/// <summary>
	/// Creates the required number of random <see cref="FavoriteSelection" /> objects.
	/// </summary>
	public static IEnumerable<FavoriteSelection> CreateFavoriteSelections(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return CreateFavoriteSelection();
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a <see cref="FavoriteSelection" /> with random properties.
	/// </summary>
	private static FavoriteSelection CreateFavoriteSelection()
	{
		return new()
		{
			CategoryId = Guid.NewGuid(),
			FavoriteId = Guid.NewGuid()
		};
	}
	#endregion
}
