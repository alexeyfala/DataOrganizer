using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Helpers;
using Entities.Enums;
using Entities.Models;
using Material.Colors;
using Material.Styles.Themes.Base;
using Repository.DTO;
using Shared.Common;
using SharpHook.Data;
using System;
using System.Collections.Generic;

namespace CommonTestHelpers.Helpers;

/// <summary>
/// Contains help methods for test purposes.
/// </summary>
public static class TestUtils
{
	#region Data
	/// <inheritdoc cref="Random" />
	private static readonly Random _random = new();
	#endregion

	#region Methods
	/// <summary>
	/// Creates a <see cref="CategoryFavoritePair" /> with random properties.
	/// </summary>
	public static CategoryFavoritePair CreateCategoryFavoritePair()
	{
		return new()
		{
			CategoryId = Guid.NewGuid(),
			FavoriteId = Guid.NewGuid()
		};
	}

	/// <summary>
	/// Creates the required number of random <see cref="CategoryFavoritePair" /> objects.
	/// </summary>
	public static IEnumerable<CategoryFavoritePair> CreateCategoryFavoritePairs(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return CreateCategoryFavoritePair();
		}
	}

	/// <summary>
	/// Creates the required number of random <see cref="CodeMaskPair" /> objects.
	/// </summary>
	public static IEnumerable<CodeMaskPair> CreateCodeMaskPairs(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return new()
			{
				Code = CreateRandomEnumValue<KeyCode>(),
				Mask = CreateRandomEnumValue<EventMask>()
			};
		}
	}

	/// <summary>
	/// Creates the required number of random <see cref="ContentsIsValidPair" /> objects.
	/// </summary>
	public static IEnumerable<ContentsIsValidPair> CreateContents(int count, bool isValid)
	{
		for (int i = 0; i < count; i++)
		{
			yield return new()
			{
				Id = Guid.NewGuid(),
				IsValid = isValid
			};
		}
	}

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
		Id = Guid.NewGuid(),
		Name = AppUtils.CreateRandomString(10)
	};

	/// <summary>
	/// Creates a <see cref="FileModel" /> object of <see cref="EntityType.File" /> content, with random properties.
	/// </summary>
	public static FileModel CreateFile(in Guid id = default) => new()
	{
		CreatedDate = DateTime.Now,
		EntityType = EntityType.File,
		Id = id == default ? Guid.NewGuid() : id,
		Index = CreateRandomIntFrom10To100(),
		Name = AppUtils.CreateRandomString(10),
		UpdatedDate = DateTime.Now
	};

	/// <summary>
	/// Creates a <see cref="FileModelDto" /> object with random properties.
	/// </summary>
	public static FileModelDto CreateFileDto(
		in Guid id = default,
		in bool isEdited = false,
		in bool isExecuted = false) => new()
		{
			CreatedDate = DateTime.Now,
			EntityType = EntityType.File,
			Id = id == default ? Guid.NewGuid() : id,
			Index = CreateRandomIntFrom10To100(),
			IsEdited = isEdited,
			IsExecuted = isExecuted,
			Name = AppUtils.CreateRandomString(10),
			UpdatedDate = DateTime.Now
		};

	/// <summary>
	/// Creates the required number of random <see cref="FileModel" /> objects.
	/// </summary>
	public static IEnumerable<FileModel> CreateFiles(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return CreateFile();
		}
	}

	/// <summary>
	/// Creates the required number of random <see cref="FileModelDto" /> objects.
	/// </summary>
	public static IEnumerable<FileModelDto> CreateFilesDto(
		int count,
		bool isEdited = false,
		bool isExecuted = false)
	{
		for (int i = 0; i < count; i++)
		{
			yield return CreateFileDto(isEdited: isEdited, isExecuted: isExecuted);
		}
	}

	/// <summary>
	/// Creates a <see cref="FolderModel" /> with random properties.
	/// </summary>
	public static FolderModel CreateFolder(in Guid id = default) => new()
	{
		CreatedDate = DateTime.Now,
		EntityType = EntityType.Folder,
		Id = id == default ? Guid.NewGuid() : id,
		Index = CreateRandomIntFrom10To100(),
		Name = AppUtils.CreateRandomString(10),
		UpdatedDate = DateTime.Now
	};

	/// <summary>
	/// Creates a <see cref="FolderModelDto" /> with random properties.
	/// </summary>
	public static FolderModelDto CreateFolderDto(in Guid id = default) => new()
	{
		CreatedDate = DateTime.Now,
		EntityType = EntityType.Folder,
		Id = id == default ? Guid.NewGuid() : id,
		Index = CreateRandomIntFrom10To100(),
		Name = AppUtils.CreateRandomString(10),
		UpdatedDate = DateTime.Now
	};

	/// <summary>
	/// Creates the required number of random <see cref="FolderModel" /> objects.
	/// </summary>
	public static IEnumerable<FolderModel> CreateFolders(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return CreateFolder();
		}
	}

	/// <summary>
	/// Creates the required number of random <see cref="FolderModelDto" /> objects.
	/// </summary>
	public static IEnumerable<FolderModelDto> CreateFoldersDto(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return CreateFolderDto();
		}
	}

	/// <summary>
	/// Creates the required number of random <see cref="Guid" /> objects.
	/// </summary>
	public static IEnumerable<Guid> CreateGuids(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return Guid.NewGuid();
		}
	}

	/// <summary>
	/// Creates the required number of random <see cref="HotkeyModel" /> objects.
	/// </summary>
	public static IEnumerable<HotkeyModel> CreateHotkeys(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return new()
			{
				Code = CreateRandomEnumValue<KeyCode>(),
				Id = Guid.NewGuid(),
				Mask = CreateRandomEnumValue<EventMask>(),
				OwnerId = Guid.NewGuid()
			};
		}
	}

	/// <summary>
	/// Creates the required number of random <see cref="HotkeyModelDto" /> objects.
	/// </summary>
	public static IEnumerable<HotkeyModelDto> CreateHotkeysDto(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return new()
			{
				Code = CreateRandomEnumValue<KeyCode>(),
				Id = Guid.NewGuid(),
				Mask = CreateRandomEnumValue<EventMask>(),
				OwnerId = Guid.NewGuid()
			};
		}
	}

	/// <summary>
	/// Generates a random sequence of bytes.
	/// </summary>
	public static byte[] CreateRandomBytes(in int length)
	{
		return TextHelper
			.Utf8Encoding
			.GetBytes(AppUtils.CreateRandomString(length));
	}

	/// <summary>
	/// Generates a random <see cref="double" /> number within a given range.
	/// </summary>
	/// <remarks>
	/// <see href="https://code-maze.com/csharp-random-double-range" />
	/// </remarks>
	public static double CreateRandomDouble(in double minValue, in double maxValue)
	{
		double value = _random.NextDouble();

		return minValue + (value * (maxValue - minValue));
	}

	/// <summary>
	/// Generates a random <see cref="Enum" /> value.
	/// </summary>
	public static T CreateRandomEnumValue<T>() where T : struct, Enum
	{
		T[] values = Enum.GetValues<T>();

		int randomIndex = _random.Next(values.Length);

		return (T)values.GetValue(randomIndex)!;
	}

	/// <summary>
	/// Generates a random <see cref="int" /> number within a given range.
	/// </summary>
	public static int CreateRandomInt(in int minValue, in int maxValue) => _random.Next(minValue, maxValue);

	/// <summary>
	/// Generates a random number between 10 and 100.
	/// </summary>
	public static int CreateRandomIntFrom10To100() => CreateRandomInt(10, 101);

	/// <summary>
	/// Generates a random <see cref="AppSettings" /> object.
	/// </summary>
	public static AppSettings CreateRandomSettings(in bool trackHotkeys = false) => new()
	{
		Language = "ja-JP",
		PrimaryColor = PrimaryColor.Red,
		SecondaryColor = SecondaryColor.Red,
		Theme = BaseThemeMode.Dark,
		TrackHotkeys = trackHotkeys
	};
	#endregion
}
