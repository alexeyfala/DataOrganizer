using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using Entities.Enums;
using Shared.Common;
using System;
using System.Collections.Generic;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.Factories;

/// <summary>
/// Factory methods that build explorer item transfer objects filled with random values.
/// </summary>
public static class ItemDtoFactory
{
	#region Methods
	/// <summary>
	/// Creates a <see cref="FileDto" /> object with random properties.
	/// </summary>
	public static FileDto CreateFileDto(
		in Guid id = default,
		in bool isEditing = false,
		in bool isExecuting = false,
		EncryptionStatus encryptionStatus = EncryptionStatus.None,
		EntityKind kind = EntityKind.File) => new()
		{
			CreatedAt = DateTime.Now,
			EncryptionStatus = encryptionStatus,
			Id = id == default ? Guid.NewGuid() : id,
			Index = RandomValues.CreateIntFrom10To100(),
			IsEditing = isEditing,
			IsExecuting = isExecuting,
			Kind = kind,
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
	/// Creates a password keeper <see cref="FolderDto" /> carrying a wrapped key.
	/// </summary>
	public static FolderDto CreateKeeperDto(bool isUnlocked = false)
	{
		FolderDto keeper = CreateFolderDto(
			encryptionStatus: isUnlocked ? EncryptionStatus.Decrypted : EncryptionStatus.Encrypted);

		keeper.EncryptedDek = RandomValues.CreateBytes(10);

		return keeper;
	}

	/// <summary>
	/// Creates a <see cref="FileDto" /> with the required base members populated, the given name and index 0.
	/// </summary>
	public static FileDto CreateNamedFileDto(string name = "") => new()
	{
		CreatedAt = DateTime.UtcNow,
		Id = Guid.NewGuid(),
		Index = 0,
		Kind = EntityKind.File,
		Name = name,
		UpdatedAt = DateTime.UtcNow
	};

	/// <summary>
	/// Creates a <see cref="FolderDto" /> with the required base members populated, the given name and index 0.
	/// </summary>
	public static FolderDto CreateNamedFolderDto(string name = "", byte[]? encryptedDek = null) => new()
	{
		CreatedAt = DateTime.UtcNow,
		EncryptedDek = encryptedDek,
		Id = Guid.NewGuid(),
		Index = 0,
		Kind = EntityKind.Folder,
		Name = name,
		UpdatedAt = DateTime.UtcNow
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
			Index = RandomValues.CreateIntFrom10To100(),
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
	#endregion
}
