using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Favorites;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Encryption;
using Entities.Enums;
using Entities.Models;
using Material.Colors;
using Material.Styles.Themes.Base;
using Repository.Dto;
using Repository.Services.Database;
using Serilog.Core;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestSupport;

/// <summary>
/// Factory methods that build objects filled with random values.
/// </summary>
public static class TestData
{
	#region Methods
	/// <summary>
	/// Creates a <see cref="DatabaseBackup" /> over a random path.
	/// </summary>
	public static DatabaseBackup CreateDatabaseBackup(IFileSystem fileSystem)
	{
		return new(CreateRandomFileName(10), fileSystem, Logger.None);
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

	/// <summary>
	/// Creates a <see cref="FileEntity" /> with random properties.
	/// </summary>
	public static FileEntity CreateFile(in Guid id = default) => new()
	{
		CreatedAt = DateTime.Now,
		Id = id == default ? Guid.NewGuid() : id,
		Index = CreateRandomIntFrom10To100(),
		Kind = EntityKind.File,
		Name = RandomString.Create(10),
		UpdatedAt = DateTime.Now
	};

	/// <summary>
	/// Creates a <see cref="FileDto" /> object with random properties.
	/// </summary>
	public static FileDto CreateFileDto(
		in Guid id = default,
		in bool isEditing = false,
		in bool isExecuting = false,
		EncryptionStatus encryptionStatus = EncryptionStatus.None) => new()
		{
			CreatedAt = DateTime.Now,
			EncryptionStatus = encryptionStatus,
			Id = id == default ? Guid.NewGuid() : id,
			Index = CreateRandomIntFrom10To100(),
			IsEditing = isEditing,
			IsExecuting = isExecuting,
			Kind = EntityKind.File,
			Name = RandomString.Create(10),
			UpdatedAt = DateTime.Now
		};

	/// <summary>
	/// Creates the required number of random <see cref="FileDto" /> objects.
	/// </summary>
	public static IEnumerable<FileDto> CreateFileDtos(
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
	/// Creates the required number of random <see cref="FileEntity" /> objects.
	/// </summary>
	public static IEnumerable<FileEntity> CreateFiles(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return CreateFile();
		}
	}

	/// <summary>
	/// Creates a <see cref="FolderEntity" /> with random properties.
	/// </summary>
	public static FolderEntity CreateFolder(in Guid id = default) => new()
	{
		CreatedAt = DateTime.Now,
		Id = id == default ? Guid.NewGuid() : id,
		Index = CreateRandomIntFrom10To100(),
		Kind = EntityKind.Folder,
		Name = RandomString.Create(10),
		UpdatedAt = DateTime.Now
	};

	/// <summary>
	/// Creates a <see cref="FolderDto" /> with random properties.
	/// </summary>
	public static FolderDto CreateFolderDto(
		in Guid id = default,
		EncryptionStatus encryptionStatus = EncryptionStatus.None) => new()
		{
			CreatedAt = DateTime.Now,
			EncryptionStatus = encryptionStatus,
			Id = id == default ? Guid.NewGuid() : id,
			Index = CreateRandomIntFrom10To100(),
			Kind = EntityKind.Folder,
			Name = RandomString.Create(10),
			UpdatedAt = DateTime.Now
		};

	/// <summary>
	/// Creates the required number of random <see cref="FolderDto" /> objects.
	/// </summary>
	public static IEnumerable<FolderDto> CreateFolderDtos(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return CreateFolderDto();
		}
	}

	/// <summary>
	/// Creates the required number of random <see cref="FolderEntity" /> objects.
	/// </summary>
	public static IEnumerable<FolderEntity> CreateFolders(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return CreateFolder();
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
	/// Creates the required number of random <see cref="HotkeyDto" /> objects.
	/// </summary>
	public static IEnumerable<HotkeyDto> CreateHotkeyDtos(int count)
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
	/// Creates the required number of random <see cref="HotkeyEntity" /> objects.
	/// </summary>
	public static IEnumerable<HotkeyEntity> CreateHotkeys(int count)
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
	/// Creates the required number of random <see cref="KeyStroke" /> objects.
	/// </summary>
	public static IEnumerable<KeyStroke> CreateKeyStrokes(int count)
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
		return $"{RandomString.Create(6)}_directory";
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
	/// Returns a random enum value other than <paramref name="excluded" />.
	/// </summary>
	public static T CreateRandomEnumValueExcept<T>(T excluded) where T : Enum
	{
		T[] filtered = [.. Enum
			.GetValues(typeof(T))
			.Cast<T>()
			.Where(value => !EqualityComparer<T>.Default.Equals(value, excluded))];

		if (filtered.IsEmpty())
		{
			throw new InvalidOperationException("No enum values available to select after exclusion.");
		}

		int index = Random
			.Shared
			.Next(0, filtered.Length);

		return filtered[index];
	}

	/// <summary>
	/// Generates a random file name with the given extension.
	/// </summary>
	public static string CreateRandomFileName(int length, string extension)
	{
		return $"{RandomString.Create(length)}_file{extension}";
	}

	/// <summary>
	/// Generates a random file name with a random extension.
	/// </summary>
	public static string CreateRandomFileName(int length)
	{
		return $"{RandomString.Create(length)}_file.{RandomString.Create(3).ToLower()}";
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
	/// Creates a sequence of the required length from a factory.
	/// </summary>
	public static IEnumerable<T> CreateSequence<T>(Func<T> factory, int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return factory();
		}
	}

	/// <summary>
	/// Creates an <see cref="AppSettings" /> object with fixed values.
	/// </summary>
	public static AppSettings CreateSettings(in bool trackHotkeys = false) => new()
	{
		Language = "ja-JP",
		PrimaryColor = PrimaryColor.Red,
		SecondaryColor = SecondaryColor.Red,
		Theme = BaseThemeMode.Dark,
		TrackHotkeys = trackHotkeys
	};

	/// <summary>
	/// Creates the required number of random <see cref="ValidatedContents" /> objects.
	/// </summary>
	public static IEnumerable<ValidatedContents> CreateValidatedContents(
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

	/// <summary>
	/// Generates a random <see cref="Enum" /> value.
	/// </summary>
	private static T CreateRandomEnumValue<T>() where T : struct, Enum
	{
		T[] values = Enum.GetValues<T>();

		int randomIndex = Random
			.Shared
			.Next(values.Length);

		return (T)values.GetValue(randomIndex)!;
	}
	#endregion
}
