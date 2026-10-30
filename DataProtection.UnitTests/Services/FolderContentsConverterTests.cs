using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataProtection.Dto;
using DataProtection.Helpers;
using DataProtection.Interfaces;
using DataProtection.Services;
using DataProtection.UnitTests.Factories;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using TestSupport.Common;
using TestSupport.Database;

namespace DataProtection.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(FolderContentsConverter)}"" type")]
internal class FolderContentsConverterTests
{
	#region Methods
	/// <summary>
	/// <see cref="FolderContentsConverter.ConvertAsync" />: contents and notes are encrypted for an encryption and
	/// decrypted for a decryption.
	/// </summary>
	[Test]
	public async Task ConvertAsync_Converts_In_The_Asked_Direction([Values] bool encrypt)
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.Note = RandomValues.CreateBytes(10);

		FileDto[] files = [ItemDtoFactory.CreateFileDto()];

		ValidatedContents[] encrypted = [.. DatabaseFactory.CreateValidatedContents(files.Length, isValid: true)];

		ValidatedContents[] decrypted = [.. DatabaseFactory.CreateValidatedContents(files.Length, isValid: true)];

		byte[] encryptedNote = RandomValues.CreateBytes(10);

		byte[] decryptedNote = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDekCipher encryption = Substitute.For<IDekCipher>();

			encryption
				.EncryptContents(Arg.Any<ValidatedContents[]>(), Arg.Any<PinnedBuffer>())
				.Returns(encrypted);

			encryption
				.DecryptContents(Arg.Any<ValidatedContents[]>(), Arg.Any<PinnedBuffer>())
				.Returns(decrypted);

			encryption
				.EncryptWithDek(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(encryptedNote);

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(decryptedNote);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsRangeAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(DatabaseFactory.CreateValidatedContents(files.Length, isValid: true).ToAsyncEnumerable());

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		FolderContentsConverter sut = mock.Create<FolderContentsConverter>();

		using PinnedBuffer dek = SecretFactory.CreateRandomKey(32);

		FolderConversionParameters parameters = new()
		{
			Dek = dek,
			Encrypt = encrypt,
			Files = files,
			Folder = folder
		};

		// Act
		FolderConversion? result = await sut.ConvertAsync(parameters);

		// Assert
		result!.Converted
			.Should()
			.Equal(encrypt ? encrypted : decrypted);

		result.Notes
			.Should()
			.ContainSingle()
			.Which
			.Note
			.Should()
			.BeSameAs(encrypt ? encryptedNote : decryptedNote);
	}

	/// <summary>
	/// <see cref="FolderContentsConverter.ConvertAsync" />: the notes of the folder, of its subfolders
	/// and of its files are all converted.
	/// </summary>
	[Test]
	public async Task ConvertAsync_Converts_The_Notes_Of_The_Folder_And_Its_Objects([Values] bool encrypt)
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.Note = RandomValues.CreateBytes(10);

		FolderDto subfolder = ItemDtoFactory.CreateFolderDto();

		subfolder.Note = RandomValues.CreateBytes(10);

		folder
			.Children
			.Add(subfolder);

		FileDto file = ItemDtoFactory.CreateFileDto();

		file.Note = RandomValues.CreateBytes(10);

		FileDto[] files = [file];

		byte[] processedNote = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDekCipher encryption = Substitute.For<IDekCipher>();

			encryption
				.DecryptContents(Arg.Any<ValidatedContents[]>(), Arg.Any<PinnedBuffer>())
				.Returns([.. DatabaseFactory.CreateValidatedContents(files.Length, isValid: true)]);

			encryption
				.EncryptContents(Arg.Any<ValidatedContents[]>(), Arg.Any<PinnedBuffer>())
				.Returns([.. DatabaseFactory.CreateValidatedContents(files.Length, isValid: true)]);

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(processedNote);

			encryption
				.EncryptWithDek(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(processedNote);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsRangeAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(DatabaseFactory.CreateValidatedContents(files.Length, isValid: true).ToAsyncEnumerable());

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		FolderContentsConverter sut = mock.Create<FolderContentsConverter>();

		using PinnedBuffer dek = SecretFactory.CreateRandomKey(32);

		FolderConversionParameters parameters = new()
		{
			Dek = dek,
			Encrypt = encrypt,
			Files = files,
			Folder = folder
		};

		// Act
		FolderConversion? result = await sut.ConvertAsync(parameters);

		// Assert
		result
			.Should()
			.NotBeNull();

		Guid[] expected = [folder.Id, subfolder.Id, file.Id];

		result.Notes
			.Select(x => x.Id)
			.Should()
			.BeEquivalentTo(expected);

		result.Notes
			.Should()
			.OnlyContain(x => x.Note == processedNote);
	}

	/// <summary>
	/// <see cref="FolderContentsConverter.ConvertAsync" />: a handed over conversion keeps the buffers
	/// of both sides intact, because its receiver reads them.
	/// </summary>
	[Test]
	public async Task ConvertAsync_Hands_Over_The_Contents_It_Converted()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto[] files = [ItemDtoFactory.CreateFileDto()];

		ValidatedContents[] loaded =
		[
			new()
			{
				Contents = [1, 2, 3, 4],
				Id = Guid.NewGuid(),
				IsValid = true
			}
		];

		ValidatedContents[] converted =
		[
			new()
			{
				Contents = [5, 6, 7, 8],
				Id = loaded[0].Id,
				IsValid = true
			}
		];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDekCipher encryption = Substitute.For<IDekCipher>();

			encryption
				.DecryptContents(Arg.Any<ValidatedContents[]>(), Arg.Any<PinnedBuffer>())
				.Returns(converted);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsRangeAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(loaded.ToAsyncEnumerable());

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		FolderContentsConverter sut = mock.Create<FolderContentsConverter>();

		using PinnedBuffer dek = SecretFactory.CreateRandomKey(32);

		FolderConversionParameters parameters = new()
		{
			Dek = dek,
			Encrypt = false,
			Files = files,
			Folder = folder
		};

		// Act
		FolderConversion? result = await sut.ConvertAsync(parameters);

		// Assert
		result
			.Should()
			.NotBeNull();

		result.Converted[0].Contents
			.Should()
			.Equal(5, 6, 7, 8);

		result.Loaded[0].Contents
			.Should()
			.Equal(1, 2, 3, 4);
	}

	/// <summary>
	/// <see cref="FolderContentsConverter.ConvertAsync" />: contents that do not come out of the
	/// conversion readable end it without a result.
	/// </summary>
	[Test]
	public async Task ConvertAsync_Refuses_A_Content_That_Cannot_Be_Converted()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto[] files = [ItemDtoFactory.CreateFileDto()];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDekCipher encryption = Substitute.For<IDekCipher>();

			encryption
				.DecryptContents(Arg.Any<ValidatedContents[]>(), Arg.Any<PinnedBuffer>())
				.Returns([.. DatabaseFactory.CreateValidatedContents(files.Length, isValid: false)]);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsRangeAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(DatabaseFactory.CreateValidatedContents(files.Length, isValid: true).ToAsyncEnumerable());

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		FolderContentsConverter sut = mock.Create<FolderContentsConverter>();

		using PinnedBuffer dek = SecretFactory.CreateRandomKey(32);

		FolderConversionParameters parameters = new()
		{
			Dek = dek,
			Encrypt = false,
			Files = files,
			Folder = folder
		};

		// Act
		FolderConversion? result = await sut.ConvertAsync(parameters);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FolderContentsConverter.ConvertAsync" />: one damaged content among readable ones ends it without a
	/// result.
	/// </summary>
	[Test]
	public async Task ConvertAsync_Refuses_Contents_Of_Which_One_Arrives_Damaged()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		IDekCipher encryption = Substitute.For<IDekCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ValidatedContents[] loaded =
			[
				.. DatabaseFactory.CreateValidatedContents(1, isValid: true),
				.. DatabaseFactory.CreateValidatedContents(1, isValid: false)
			];

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsRangeAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(loaded.ToAsyncEnumerable());

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		FolderContentsConverter sut = mock.Create<FolderContentsConverter>();

		using PinnedBuffer dek = SecretFactory.CreateRandomKey(32);

		FolderConversionParameters parameters = new()
		{
			Dek = dek,
			Encrypt = false,
			Files = files,
			Folder = folder
		};

		// Act
		FolderConversion? result = await sut.ConvertAsync(parameters);

		// Assert
		result
			.Should()
			.BeNull();

		encryption
			.DidNotReceiveWithAnyArgs()
			.DecryptContents(default!, default!);
	}

	/// <summary>
	/// <see cref="FolderContentsConverter.ConvertAsync" />: contents that the database reports as
	/// unreadable, or that arrive without an identifier, end it without a result.
	/// </summary>
	[Test]
	[TestCase(false, true)]
	[TestCase(true, false)]
	public async Task ConvertAsync_Refuses_Contents_That_Arrive_Damaged(bool isValid, bool hasId)
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto[] files = [ItemDtoFactory.CreateFileDto()];

		ValidatedContents[] loaded =
		[
			new()
			{
				Contents = RandomValues.CreateBytes(10),
				Id = hasId ? Guid.NewGuid() : Guid.Empty,
				IsValid = isValid
			}
		];

		IDekCipher encryption = Substitute.For<IDekCipher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsRangeAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(loaded.ToAsyncEnumerable());

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		FolderContentsConverter sut = mock.Create<FolderContentsConverter>();

		using PinnedBuffer dek = SecretFactory.CreateRandomKey(32);

		FolderConversionParameters parameters = new()
		{
			Dek = dek,
			Encrypt = false,
			Files = files,
			Folder = folder
		};

		// Act
		FolderConversion? result = await sut.ConvertAsync(parameters);

		// Assert
		result
			.Should()
			.BeNull();

		encryption
			.DidNotReceiveWithAnyArgs()
			.DecryptContents(default!, default!);
	}

	/// <summary>
	/// <see cref="FolderContentsConverter.ConvertAsync" />: the contents decrypted so far are erased when a note cannot be
	/// converted.
	/// </summary>
	[Test]
	public async Task ConvertAsync_Wipes_The_Converted_Contents_When_A_Note_Cannot_Be_Converted()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.Note = RandomValues.CreateBytes(10);

		FileDto[] files = [ItemDtoFactory.CreateFileDto()];

		ValidatedContents[] decrypted =
		[
			new()
			{
				Contents = [1, 2, 3, 4],
				Id = Guid.NewGuid(),
				IsValid = true
			}
		];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDekCipher encryption = Substitute.For<IDekCipher>();

			encryption
				.DecryptContents(Arg.Any<ValidatedContents[]>(), Arg.Any<PinnedBuffer>())
				.Returns(decrypted);

			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(_ => throw new CryptographicException());

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsRangeAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(DatabaseFactory.CreateValidatedContents(files.Length, isValid: true).ToAsyncEnumerable());

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		FolderContentsConverter sut = mock.Create<FolderContentsConverter>();

		using PinnedBuffer dek = SecretFactory.CreateRandomKey(32);

		FolderConversionParameters parameters = new()
		{
			Dek = dek,
			Encrypt = false,
			Files = files,
			Folder = folder
		};

		// Act
		Func<Task> act = () => sut.ConvertAsync(parameters);

		// Assert
		await act
			.Should()
			.ThrowExactlyAsync<CryptographicException>();

		decrypted[0].Contents
			.Should()
			.AllSatisfy(x => x
				.Should()
				.Be(0));
	}

	/// <summary>
	/// <see cref="FolderContentsConverter.ConvertAsync" />: the plain text that was loaded to be
	/// encrypted is erased when the conversion ends without a result.
	/// </summary>
	[Test]
	public async Task ConvertAsync_Wipes_The_Loaded_Contents_When_A_Content_Cannot_Be_Converted()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto[] files = [ItemDtoFactory.CreateFileDto()];

		ValidatedContents[] loaded =
		[
			new()
			{
				Contents = [1, 2, 3, 4],
				Id = Guid.NewGuid(),
				IsValid = true
			}
		];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDekCipher encryption = Substitute.For<IDekCipher>();

			encryption
				.EncryptContents(Arg.Any<ValidatedContents[]>(), Arg.Any<PinnedBuffer>())
				.Returns([.. DatabaseFactory.CreateValidatedContents(files.Length, isValid: false)]);

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsRangeAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(loaded.ToAsyncEnumerable());

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		FolderContentsConverter sut = mock.Create<FolderContentsConverter>();

		using PinnedBuffer dek = SecretFactory.CreateRandomKey(32);

		FolderConversionParameters parameters = new()
		{
			Dek = dek,
			Encrypt = true,
			Files = files,
			Folder = folder
		};

		// Act
		await sut.ConvertAsync(parameters);

		// Assert
		loaded[0].Contents
			.Should()
			.AllSatisfy(x => x
				.Should()
				.Be(0));
	}

	/// <summary>
	/// <see cref="FolderContentsConverter.ConvertAsync" />: a note that cannot be converted leaves
	/// none of the notes converted before it in plain text.
	/// </summary>
	[Test]
	public async Task ConvertAsync_Wipes_The_Notes_When_One_Cannot_Be_Converted()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.Note = RandomValues.CreateBytes(10);

		FolderDto subfolder = ItemDtoFactory.CreateFolderDto();

		subfolder.Note = RandomValues.CreateBytes(10);

		folder
			.Children
			.Add(subfolder);

		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(1)];

		byte[] decryptedNote = [5, 6, 7, 8];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDekCipher encryption = Substitute.For<IDekCipher>();

			encryption
				.DecryptContents(Arg.Any<ValidatedContents[]>(), Arg.Any<PinnedBuffer>())
				.Returns([.. DatabaseFactory.CreateValidatedContents(files.Length, isValid: true)]);

			// The first note opens, the second one does not.
			encryption
				.DecryptWithDek(Arg.Any<byte[]>(), Arg.Any<PinnedBuffer>(), Arg.Any<ContentIdentity>())
				.Returns(
					_ => decryptedNote,
					_ => throw new CryptographicException());

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsRangeAsync(Arg.Any<IEnumerable<Guid>>())
				.Returns(DatabaseFactory.CreateValidatedContents(files.Length, isValid: true).ToAsyncEnumerable());

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(dbAccess);
		});

		FolderContentsConverter sut = mock.Create<FolderContentsConverter>();

		using PinnedBuffer dek = SecretFactory.CreateRandomKey(32);

		FolderConversionParameters parameters = new()
		{
			Dek = dek,
			Encrypt = false,
			Files = files,
			Folder = folder
		};

		// Act
		Func<Task> act = () => sut.ConvertAsync(parameters);

		// Assert
		await act
			.Should()
			.ThrowExactlyAsync<CryptographicException>();

		decryptedNote
			.Should()
			.AllSatisfy(x => x
				.Should()
				.Be(0));
	}
	#endregion
}
