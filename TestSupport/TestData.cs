using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Favorites;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums;
using Entities.Enums;
using Entities.Models;
using Material.Colors;
using Material.Styles.Themes.Base;
using Repository.Dto;
using Repository.Services;
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
/// Contains help methods for test purposes.
/// </summary>
public static class TestData
{
	#region Methods
	/// <summary>
	/// Creates a <see cref="FavoriteSelection" /> with random properties.
	/// </summary>
	public static FavoriteSelection CreateFavoriteSelection()
	{
		return new()
		{
			CategoryId = Guid.NewGuid(),
			FavoriteId = Guid.NewGuid()
		};
	}

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
	/// Creates the required number of random <see cref="ValidatedContents" /> objects.
	/// </summary>
	public static IEnumerable<ValidatedContents> CreateContents(
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
	/// Creates a <see cref="FileEntity" /> object of <see cref="EntityKind.File" /> content, with random properties.
	/// </summary>
	public static FileEntity CreateFile(in Guid id = default) => new()
	{
		CreatedDate = DateTime.Now,
		EntityType = EntityKind.File,
		Id = id == default ? Guid.NewGuid() : id,
		Index = CreateRandomIntFrom10To100(),
		Name = RandomString.Create(10),
		UpdatedDate = DateTime.Now
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
			CreatedDate = DateTime.Now,
			EncryptionStatus = encryptionStatus,
			EntityType = EntityKind.File,
			Id = id == default ? Guid.NewGuid() : id,
			Index = CreateRandomIntFrom10To100(),
			IsEditing = isEditing,
			IsExecuting = isExecuting,
			Name = RandomString.Create(10),
			UpdatedDate = DateTime.Now
		};

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
	/// Creates the required number of random <see cref="FileDto" /> objects.
	/// </summary>
	public static IEnumerable<FileDto> CreateFilesDto(
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
	/// Creates a <see cref="FolderEntity" /> with random properties.
	/// </summary>
	public static FolderEntity CreateFolder(in Guid id = default) => new()
	{
		CreatedDate = DateTime.Now,
		EntityType = EntityKind.Folder,
		Id = id == default ? Guid.NewGuid() : id,
		Index = CreateRandomIntFrom10To100(),
		Name = RandomString.Create(10),
		UpdatedDate = DateTime.Now
	};

	/// <summary>
	/// Creates a <see cref="FolderDto" /> with random properties.
	/// </summary>
	public static FolderDto CreateFolderDto(
		in Guid id = default,
		EncryptionStatus encryptionStatus = EncryptionStatus.None) => new()
		{
			CreatedDate = DateTime.Now,
			EncryptionStatus = encryptionStatus,
			EntityType = EntityKind.Folder,
			Id = id == default ? Guid.NewGuid() : id,
			Index = CreateRandomIntFrom10To100(),
			Name = RandomString.Create(10),
			UpdatedDate = DateTime.Now
		};

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
	/// Creates the required number of random <see cref="FolderDto" /> objects.
	/// </summary>
	public static IEnumerable<FolderDto> CreateFoldersDto(int count)
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
	/// Creates the required number of random <see cref="HotkeyDto" /> objects.
	/// </summary>
	public static IEnumerable<HotkeyDto> CreateHotkeysDto(int count)
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
		return $"{RandomString.Create(length)}_file{extension}";
	}

	/// <summary>
	/// Generates a random file name.
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
