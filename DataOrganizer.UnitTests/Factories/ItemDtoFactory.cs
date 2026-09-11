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
		EncryptionStatus encryptionStatus = EncryptionStatus.None) => new()
		{
			CreatedAt = DateTime.Now,
			EncryptionStatus = encryptionStatus,
			Id = id == default ? Guid.NewGuid() : id,
			Index = RandomValues.CreateIntFrom10To100(),
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
