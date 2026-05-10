using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Enums;
using Entities.Enums;
using Entities.Models;
using Material.Colors;
using Material.Styles.Themes.Base;
using Repository.DTO;
using Shared.Common;
using Shared.Extensions;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonTestHelpers.Helpers;

/// <summary>
/// Contains help methods for test purposes.
/// </summary>
public static class TestUtils
{
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
	public static IEnumerable<ContentsIsValidPair> CreateContents(
		int count,
		bool isValid,
		bool generateId = true)
	{
		for (int i = 0; i < count; i++)
		{
			yield return new()
			{
				Id = generateId ? Guid.NewGuid() : default,
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
		EncryptionStatus = EncryptionStatus.None,
		Id = Guid.NewGuid(),
		Index = default,
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
		in bool isEditing = false,
		in bool isExecuting = false,
		EncryptionStatus encryptionStatus = EncryptionStatus.None) => new()
		{
			CreatedDate = DateTime.Now,
			EncryptionStatus = encryptionStatus,
			EntityType = EntityType.File,
			Id = id == default ? Guid.NewGuid() : id,
			Index = CreateRandomIntFrom10To100(),
			IsEditing = isEditing,
			IsExecuting = isExecuting,
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
		bool isEditing = false,
		bool isExecuting = false,
		EncryptionStatus encryptionStatus = EncryptionStatus.None)
	{
		for (int i = 0; i < count; i++)
		{
			yield return CreateFileDto(
				isEditing: isEditing,
				isExecuting: isExecuting,
				encryptionStatus: encryptionStatus);
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
	public static FolderModelDto CreateFolderDto(
		in Guid id = default,
		EncryptionStatus encryptionStatus = EncryptionStatus.None) => new()
		{
			CreatedDate = DateTime.Now,
			EncryptionStatus = encryptionStatus,
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
				Index = default,
				Mask = CreateRandomEnumValue<EventMask>(),
				OwnerId = Guid.NewGuid()
			};
		}
	}

	/// <summary>
	/// Generates a random sequence of bytes.
	/// </summary>
	public static byte[] CreateRandomBytes(int length)
	{
		byte[] buffer = new byte[length];

		Random
			.Shared
			.NextBytes(buffer);

		return buffer;
	}

	/// <summary>
	/// Generates a random directory name.
	/// </summary>
	public static string CreateRandomDirectoryName()
	{
		return $"{AppUtils.CreateRandomString(6)}_directory";
	}

	/// <summary>
	/// Generates a random <see cref="double" /> number within a given range.
	/// </summary>
	/// <remarks>
	/// <see href="https://code-maze.com/csharp-random-double-range" />
	/// </remarks>
	public static double CreateRandomDouble(double minValue, double maxValue)
	{
		double value = Random
			.Shared
			.NextDouble();

		return minValue + (value * (maxValue - minValue));
	}

	/// <summary>
	/// Generates a random <see cref="Enum" /> value.
	/// </summary>
	public static T CreateRandomEnumValue<T>() where T : struct, Enum
	{
		T[] values = Enum.GetValues<T>();

		int randomIndex = Random
			.Shared
			.Next(values.Length);

		return (T)values.GetValue(randomIndex)!;
	}

	/// <summary>
	/// Generates a random file name.
	/// </summary>
	public static string CreateRandomFileName(int length, string extension)
	{
		return $"{AppUtils.CreateRandomString(length)}_file{extension}";
	}

	/// <summary>
	/// Generates a random <see cref="int" /> number within a given range.
	/// </summary>
	public static int CreateRandomInt(int minValue, int maxValue)
	{
		return Random
			.Shared
			.Next(minValue, maxValue);
	}

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

	/// <summary>
	/// Generates a sequence of the required length by calling <see cref="Func{T}" /> from the argument.
	/// </summary>
	public static IEnumerable<T> CreateSequence<T>(Func<T> action, int length)
	{
		for (int i = 0; i < length; i++)
		{
			yield return action();
		}
	}

	/// <summary>
	/// Returns a random value from enum except defined in <paramref name="toExclude"/>.
	/// </summary>
	public static T GetRandomEnumValueExcept<T>(T toExclude) where T : Enum
	{
		T[] filtered = [.. Enum
			.GetValues(typeof(T))
			.Cast<T>()
			.Where(value => !EqualityComparer<T>.Default.Equals(value, toExclude))];

		if (filtered.IsEmpty())
		{
			throw new InvalidOperationException("No enum values available to select after exclusion.");
		}

		int index = Random
			.Shared
			.Next(0, filtered.Length);

		return filtered[index];
	}
	#endregion
}
