using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataProtection.Helpers;
using DataProtection.Interfaces;
using DataProtection.Services;
using DataProtection.UnitTests.Factories;
using Hierarchy.Dto;
using Hierarchy.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Security.Cryptography;
using TestSupport.Common;

namespace DataProtection.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(ContentCipher)}"" type")]
internal class ContentCipherTests
{
	#region Methods
	/// <summary>
	/// <see cref="ContentCipher.Decrypt" />: empty contents are stored unencrypted, so they come
	/// back untouched and the key store stays out of it.
	/// </summary>
	[Test]
	public void Decrypt_Hands_Empty_Contents_Back()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(sessionKeyStore));

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[] output = sut.Decrypt(file, []);

		// Assert
		output
			.Should()
			.BeEmpty();

		sessionKeyStore
			.DidNotReceiveWithAnyArgs()
			.Decrypt(default, default, default!);
	}

	/// <summary>
	/// <see cref="ContentCipher.Decrypt" />: returns non-empty contents that differ from the input.
	/// </summary>
	[Test]
	public void Decrypt_Returns_Contents_Different_From_The_Input()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.EncryptedDek = RandomValues.CreateBytes(10);

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		byte[] contents = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Decrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(sessionKeyStore);
		});

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[] output = sut.Decrypt(file, contents);

		// Assert
		output
			.Should()
			.NotBeNullOrEmpty();

		output
			.Should()
			.NotBeEquivalentTo(contents);
	}

	/// <summary>
	/// <see cref="ContentCipher.TryDecrypt" />: hands the plain text of the key store over.
	/// </summary>
	[Test]
	public void TryDecrypt_Hands_The_Plain_Text_Over()
	{
		// Arrange
		Guid keeperId = Guid.NewGuid();

		byte[] input = RandomValues.CreateBytes(10);

		byte[] decrypted = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Decrypt(keeperId, Arg.Any<ContentIdentity>(), input)
				.Returns(decrypted);

			builder.RegisterInstance(sessionKeyStore);
		});

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[]? result = sut.TryDecrypt(
			keeperId,
			ContentIdentity.Note,
			input);

		// Assert
		result
			.Should()
			.BeSameAs(decrypted);
	}

	/// <summary>
	/// <see cref="ContentCipher.TryDecrypt" />: a locked keeper or damaged data ends with a refusal
	/// instead of an exception, since the caller renders the content.
	/// </summary>
	[Test]
	[TestCaseSource(nameof(SessionCipherFailures))]
	public void TryDecrypt_Refuses_On_A_Failure(Exception failure)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Decrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Throws(failure);

			builder.RegisterInstance(sessionKeyStore);
		});

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[]? result = sut.TryDecrypt(
			Guid.NewGuid(),
			ContentIdentity.Note,
			RandomValues.CreateBytes(10));

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ContentCipher.TryEncrypt" />: a locked keeper ends with a refusal instead of an exception.
	/// </summary>
	[Test]
	[TestCaseSource(nameof(SessionCipherFailures))]
	public void TryEncrypt_Refuses_On_A_Failure(Exception failure)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

			sessionKeyStore
				.Encrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>())
				.Throws(failure);

			builder.RegisterInstance(sessionKeyStore);
		});

		ContentCipher sut = mock.Create<ContentCipher>();

		// Act
		byte[]? result = sut.TryEncrypt(
			Guid.NewGuid(),
			ContentIdentity.Note,
			RandomValues.CreateBytes(10));

		// Assert
		result
			.Should()
			.BeNull();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Failures an operation on the key of a session can end with.
	/// </summary>
	private static Exception[] SessionCipherFailures() =>
	[
		new AuthenticationTagMismatchException(),
		new InvalidOperationException()
	];
	#endregion
}
