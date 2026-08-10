using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Encryption;
using System;

namespace DataOrganizer.UnitTests.TestTypes.Security;

[TestFixture(Description = $@"Tests of ""{nameof(SessionKeyStore)}"" type")]
internal class SessionKeyStoreTests
{
	#region Data
	/// <summary>
	/// Key size of the AEAD algorithm behind the encryption service.
	/// </summary>
	private const int DekSize = 32;
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="SessionKeyStore.Decrypt" />: returns null once the keeper has been locked.
	/// </summary>
	[Test]
	public void Decrypt_Returns_Null_After_Lock()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().As<IEncryptionService>());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid keeperId = Guid.NewGuid();

		sut.Unlock(keeperId, TestUtils.CreateRandomBytes(DekSize))
			.Should()
			.BeTrue();

		byte[]? encrypted = sut.Encrypt(keeperId, TestUtils.CreateRandomBytes(64));

		encrypted
			.Should()
			.NotBeNull();

		// Act
		sut.Lock(keeperId);

		// Assert
		sut.Decrypt(keeperId, encrypted!)
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Decrypt" />: returns null for a keeper that was never unlocked.
	/// </summary>
	[Test]
	public void Decrypt_Returns_Null_For_Unknown_Keeper()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().As<IEncryptionService>());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		// Act, Assert
		sut.Decrypt(Guid.NewGuid(), TestUtils.CreateRandomBytes(64))
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Encrypt" />, <see cref="SessionKeyStore.Decrypt" />: contents survive a round trip through an unlocked keeper.
	/// </summary>
	[Test]
	public void Encrypt_Decrypt_Round_Trips_Contents()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().As<IEncryptionService>());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid keeperId = Guid.NewGuid();

		byte[] contents = TestUtils.CreateRandomBytes(128);

		sut.Unlock(keeperId, TestUtils.CreateRandomBytes(DekSize))
			.Should()
			.BeTrue();

		// Act
		byte[]? encrypted = sut.Encrypt(keeperId, contents);

		encrypted
			.Should()
			.NotBeNull()
			.And
			.NotEqual(contents);

		byte[]? decrypted = sut.Decrypt(keeperId, encrypted);

		// Assert
		decrypted
			.Should()
			.Equal(contents);
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Encrypt" />: returns null for a keeper that is not unlocked.
	/// </summary>
	[Test]
	public void Encrypt_Returns_Null_For_Locked_Keeper()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().As<IEncryptionService>());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		// Act, Assert
		sut.Encrypt(Guid.NewGuid(), TestUtils.CreateRandomBytes(64))
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SessionKeyStore.IsUnlocked" />: follows the unlocked state of a keeper.
	/// </summary>
	[Test]
	public void IsUnlocked_Follows_Unlock_And_Lock()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().As<IEncryptionService>());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid keeperId = Guid.NewGuid();

		// Act, Assert
		sut.IsUnlocked(keeperId)
			.Should()
			.BeFalse();

		sut.Unlock(keeperId, TestUtils.CreateRandomBytes(DekSize));

		sut.IsUnlocked(keeperId)
			.Should()
			.BeTrue();

		sut.Lock(keeperId);

		sut.IsUnlocked(keeperId)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// A key is bound to its keeper: contents of one keeper cannot be read through another one.
	/// </summary>
	[Test]
	public void Keepers_Are_Isolated()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().As<IEncryptionService>());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid firstKeeperId = Guid.NewGuid();

		Guid secondKeeperId = Guid.NewGuid();

		sut.Unlock(firstKeeperId, TestUtils.CreateRandomBytes(DekSize));

		sut.Unlock(secondKeeperId, TestUtils.CreateRandomBytes(DekSize));

		// Act
		byte[]? encrypted = sut.Encrypt(firstKeeperId, TestUtils.CreateRandomBytes(64));

		// Assert
		sut.Decrypt(secondKeeperId, encrypted!)
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Lock" />: locking one keeper leaves the keys of the others usable.
	/// </summary>
	[Test]
	public void Lock_Keeps_Other_Keepers_Usable()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().As<IEncryptionService>());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid firstKeeperId = Guid.NewGuid();

		Guid secondKeeperId = Guid.NewGuid();

		byte[] contents = TestUtils.CreateRandomBytes(64);

		sut.Unlock(firstKeeperId, TestUtils.CreateRandomBytes(DekSize));

		sut.Unlock(secondKeeperId, TestUtils.CreateRandomBytes(DekSize));

		byte[]? encrypted = sut.Encrypt(secondKeeperId, contents);

		// Act
		sut.Lock(firstKeeperId);

		// Assert
		sut.Decrypt(secondKeeperId, encrypted!)
			.Should()
			.Equal(contents);
	}

	/// <summary>
	/// <see cref="SessionKeyStore.LockAll" />: drops the keys of every keeper.
	/// </summary>
	[Test]
	public void LockAll_Locks_Every_Keeper()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().As<IEncryptionService>());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid firstKeeperId = Guid.NewGuid();

		Guid secondKeeperId = Guid.NewGuid();

		sut.Unlock(firstKeeperId, TestUtils.CreateRandomBytes(DekSize));

		sut.Unlock(secondKeeperId, TestUtils.CreateRandomBytes(DekSize));

		// Act
		sut.LockAll();

		// Assert
		sut.IsUnlocked(firstKeeperId)
			.Should()
			.BeFalse();

		sut.IsUnlocked(secondKeeperId)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Unlock" />: rejects an empty key.
	/// </summary>
	[Test]
	public void Unlock_Rejects_An_Empty_Dek()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().As<IEncryptionService>());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid keeperId = Guid.NewGuid();

		// Act, Assert
		sut.Unlock(keeperId, [])
			.Should()
			.BeFalse();

		sut.IsUnlocked(keeperId)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Unlock" />: unlocking a keeper again replaces its key while the session stays alive.
	/// </summary>
	[Test]
	public void Unlock_Replaces_The_Key_Of_A_Keeper()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().As<IEncryptionService>());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid keeperId = Guid.NewGuid();

		byte[] contents = TestUtils.CreateRandomBytes(64);

		sut.Unlock(keeperId, TestUtils.CreateRandomBytes(DekSize));

		byte[]? staleEncrypted = sut.Encrypt(keeperId, contents);

		// Act
		sut.Unlock(keeperId, TestUtils.CreateRandomBytes(DekSize))
			.Should()
			.BeTrue();

		// Assert
		sut.Decrypt(keeperId, staleEncrypted!)
			.Should()
			.BeNull();

		byte[]? encrypted = sut.Encrypt(keeperId, contents);

		sut.Decrypt(keeperId, encrypted!)
			.Should()
			.Equal(contents);
	}
	#endregion
}
