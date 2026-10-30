using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataProtection.Helpers;
using DataProtection.Interfaces;
using DataProtection.Services;
using DataProtection.UnitTests.Factories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Security.Cryptography;
using TestSupport.Common;

namespace DataProtection.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(SessionKeyStore)}"" type")]
internal class SessionKeyStoreTests
{
	#region Data
	/// <summary>
	/// Key size of the AEAD algorithm behind the encryption service.
	/// </summary>
	private const int DekSize = 32;

	/// <summary>
	/// Place the contents of these tests belong to.
	/// </summary>
	private static readonly ContentIdentity Identity = ContentIdentity.Contents;
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="SessionKeyStore.Decrypt" />: refuses to work once the keeper has been locked.
	/// </summary>
	[Test]
	public void Decrypt_Refuses_After_Lock()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().AsImplementedInterfaces());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid keeperId = Guid.NewGuid();

		sut.Unlock(keeperId, SecretFactory.CreateRandomKey(DekSize))
			.Should()
			.BeTrue();

		byte[]? encrypted = sut.Encrypt(keeperId, Identity, RandomValues.CreateBytes(64));

		encrypted
			.Should()
			.NotBeNull();

		// Act
		sut.Lock(keeperId);

		// Assert
		Action act = () => sut.Decrypt(keeperId, Identity, encrypted!);

		act
			.Should()
			.ThrowExactly<InvalidOperationException>();
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Decrypt" />: a key is bound to its keeper, so contents of one keeper cannot
	/// be read through another one.
	/// </summary>
	[Test]
	public void Decrypt_Refuses_Another_Keeper()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().AsImplementedInterfaces());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid firstKeeperId = Guid.NewGuid();

		Guid secondKeeperId = Guid.NewGuid();

		sut.Unlock(firstKeeperId, SecretFactory.CreateRandomKey(DekSize));

		sut.Unlock(secondKeeperId, SecretFactory.CreateRandomKey(DekSize));

		// Act
		byte[]? encrypted = sut.Encrypt(firstKeeperId, Identity, RandomValues.CreateBytes(64));

		// Assert
		Action act = () => sut.Decrypt(secondKeeperId, Identity, encrypted!);

		act
			.Should()
			.ThrowExactly<AuthenticationTagMismatchException>();
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Decrypt" />: contents are bound to the field they are stored in, so a note
	/// cannot be read back as the contents of the same object.
	/// </summary>
	[Test]
	public void Decrypt_Refuses_Another_Purpose()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().AsImplementedInterfaces());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid keeperId = Guid.NewGuid();

		Guid fileId = Guid.NewGuid();

		sut.Unlock(keeperId, SecretFactory.CreateRandomKey(DekSize));

		// Act
		byte[]? encrypted = sut.Encrypt(
			keeperId,
			ContentIdentity.Note,
			RandomValues.CreateBytes(64));

		// Assert
		Action act = () => sut.Decrypt(keeperId, ContentIdentity.Contents, encrypted!);

		act
			.Should()
			.ThrowExactly<AuthenticationTagMismatchException>();
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Decrypt" />: refuses to work for a keeper that was never unlocked.
	/// </summary>
	[Test]
	public void Decrypt_Refuses_For_Unknown_Keeper()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().AsImplementedInterfaces());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		// Act, Assert
		Action act = () => sut.Decrypt(Guid.NewGuid(), Identity, RandomValues.CreateBytes(64));

		act
			.Should()
			.ThrowExactly<InvalidOperationException>();
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Dispose" />: leaving the process takes the keys with it.
	/// </summary>
	[Test]
	public void Dispose_Locks_Every_Keeper()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().AsImplementedInterfaces());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid keeperId = Guid.NewGuid();

		sut.Unlock(keeperId, SecretFactory.CreateRandomKey(DekSize));

		// Act
		sut.Dispose();

		// Assert
		sut.IsUnlocked(keeperId)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Encrypt" />, <see cref="SessionKeyStore.Decrypt" />: contents survive a round trip through an unlocked keeper.
	/// </summary>
	[Test]
	public void Encrypt_Decrypt_Round_Trips_Contents()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().AsImplementedInterfaces());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid keeperId = Guid.NewGuid();

		byte[] contents = RandomValues.CreateBytes(128);

		sut.Unlock(keeperId, SecretFactory.CreateRandomKey(DekSize))
			.Should()
			.BeTrue();

		// Act
		byte[]? encrypted = sut.Encrypt(keeperId, Identity, contents);

		encrypted
			.Should()
			.NotBeNull()
			.And
			.NotEqual(contents);

		byte[]? decrypted = sut.Decrypt(keeperId, Identity, encrypted);

		// Assert
		decrypted
			.Should()
			.Equal(contents);
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Encrypt" />: refuses to work for a keeper that is not unlocked.
	/// </summary>
	[Test]
	public void Encrypt_Refuses_For_Locked_Keeper()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().AsImplementedInterfaces());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		// Act, Assert
		Action act = () => sut.Encrypt(Guid.NewGuid(), Identity, RandomValues.CreateBytes(64));

		act
			.Should()
			.ThrowExactly<InvalidOperationException>();
	}

	/// <summary>
	/// <see cref="SessionKeyStore.IsUnlocked" />: follows the unlocked state of a keeper.
	/// </summary>
	[Test]
	public void IsUnlocked_Follows_Unlock_And_Lock()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().AsImplementedInterfaces());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid keeperId = Guid.NewGuid();

		// Act, Assert
		sut.IsUnlocked(keeperId)
			.Should()
			.BeFalse();

		sut.Unlock(keeperId, SecretFactory.CreateRandomKey(DekSize));

		sut.IsUnlocked(keeperId)
			.Should()
			.BeTrue();

		sut.Lock(keeperId);

		sut.IsUnlocked(keeperId)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Lock" />: locking one keeper leaves the keys of the others usable.
	/// </summary>
	[Test]
	public void Lock_Keeps_Other_Keepers_Usable()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().AsImplementedInterfaces());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid firstKeeperId = Guid.NewGuid();

		Guid secondKeeperId = Guid.NewGuid();

		byte[] contents = RandomValues.CreateBytes(64);

		sut.Unlock(firstKeeperId, SecretFactory.CreateRandomKey(DekSize));

		sut.Unlock(secondKeeperId, SecretFactory.CreateRandomKey(DekSize));

		byte[]? encrypted = sut.Encrypt(secondKeeperId, Identity, contents);

		// Act
		sut.Lock(firstKeeperId);

		// Assert
		sut.Decrypt(secondKeeperId, Identity, encrypted!)
			.Should()
			.Equal(contents);
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Lock" />: locking the last keeper wipes the session secret.
	/// </summary>
	[Test]
	public void Lock_Wipes_The_Session_Secret_With_The_Last_Keeper()
	{
		// Arrange
		PinnedBuffer? sessionId = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ISessionDekWrapper sessionDekWrapper = Substitute.For<ISessionDekWrapper>();

			sessionDekWrapper
				.EncryptWithSessionId(Arg.Any<PinnedBuffer>(), Arg.Do<PinnedBuffer>(x => sessionId = x), Arg.Any<ContentIdentity>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(sessionDekWrapper);
		});

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid keeperId = Guid.NewGuid();

		sut.Unlock(keeperId, SecretFactory.CreateRandomKey(DekSize));

		// Act
		sut.Lock(keeperId);

		// Assert
		Action read = () => sessionId!.AsReadOnlySpan();

		read
			.Should()
			.ThrowExactly<ObjectDisposedException>();
	}

	/// <summary>
	/// <see cref="SessionKeyStore.LockAll" />: drops the keys of every keeper.
	/// </summary>
	[Test]
	public void LockAll_Locks_Every_Keeper()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().AsImplementedInterfaces());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid firstKeeperId = Guid.NewGuid();

		Guid secondKeeperId = Guid.NewGuid();

		sut.Unlock(firstKeeperId, SecretFactory.CreateRandomKey(DekSize));

		sut.Unlock(secondKeeperId, SecretFactory.CreateRandomKey(DekSize));

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
	/// <see cref="SessionKeyStore.LockAll" />: wipes the session secret.
	/// </summary>
	[Test]
	public void LockAll_Wipes_The_Session_Secret()
	{
		// Arrange
		PinnedBuffer? sessionId = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ISessionDekWrapper sessionDekWrapper = Substitute.For<ISessionDekWrapper>();

			sessionDekWrapper
				.EncryptWithSessionId(Arg.Any<PinnedBuffer>(), Arg.Do<PinnedBuffer>(x => sessionId = x), Arg.Any<ContentIdentity>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(sessionDekWrapper);
		});

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		sut.Unlock(Guid.NewGuid(), SecretFactory.CreateRandomKey(DekSize));

		// Act
		sut.LockAll();

		// Assert
		Action read = () => sessionId!.AsReadOnlySpan();

		read
			.Should()
			.ThrowExactly<ObjectDisposedException>();
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Unlock" />: the session secret is drawn at random.
	/// </summary>
	[Test]
	public void Unlock_Draws_A_Random_Session_Secret()
	{
		// Arrange
		byte[] secret = [];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ISessionDekWrapper sessionDekWrapper = Substitute.For<ISessionDekWrapper>();

			sessionDekWrapper
				.EncryptWithSessionId(
					Arg.Any<PinnedBuffer>(),
					Arg.Do<PinnedBuffer>(x => secret = x.AsReadOnlySpan().ToArray()),
					Arg.Any<ContentIdentity>())
				.Returns(RandomValues.CreateBytes(10));

			builder.RegisterInstance(sessionDekWrapper);
		});

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		// Act
		sut.Unlock(Guid.NewGuid(), SecretFactory.CreateRandomKey(DekSize));

		// Assert
		secret
			.Should()
			.Contain(x => x != 0);
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Unlock" />: a key that cannot be wrapped leaves the keepers
	/// already unlocked usable.
	/// </summary>
	[Test]
	public void Unlock_Keeps_Other_Keepers_Usable_When_The_Key_Cannot_Be_Wrapped()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().AsImplementedInterfaces());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid unlockedKeeperId = Guid.NewGuid();

		Guid refusedKeeperId = Guid.NewGuid();

		byte[] contents = RandomValues.CreateBytes(64);

		sut.Unlock(unlockedKeeperId, SecretFactory.CreateRandomKey(DekSize));

		byte[]? encrypted = sut.Encrypt(unlockedKeeperId, Identity, contents);

		// Act
		// A key of another size is refused by the format, and that refusal is what the store has to survive.
		Action act = () => sut.Unlock(refusedKeeperId, SecretFactory.CreateRandomKey(DekSize / 2));

		// Assert
		act
			.Should()
			.Throw<CryptographicException>();

		sut.Decrypt(unlockedKeeperId, Identity, encrypted!)
			.Should()
			.Equal(contents);
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Unlock" />: rejects an empty key.
	/// </summary>
	[Test]
	public void Unlock_Rejects_An_Empty_Dek()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().AsImplementedInterfaces());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid keeperId = Guid.NewGuid();

		// Act, Assert
		sut.Unlock(keeperId, new PinnedBuffer(0))
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
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterType<EncryptionService>().AsImplementedInterfaces());

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		Guid keeperId = Guid.NewGuid();

		byte[] contents = RandomValues.CreateBytes(64);

		sut.Unlock(keeperId, SecretFactory.CreateRandomKey(DekSize));

		byte[]? staleEncrypted = sut.Encrypt(keeperId, Identity, contents);

		// Act
		sut.Unlock(keeperId, SecretFactory.CreateRandomKey(DekSize))
			.Should()
			.BeTrue();

		// Assert
		Action act = () => sut.Decrypt(keeperId, Identity, staleEncrypted!);

		act
			.Should()
			.ThrowExactly<AuthenticationTagMismatchException>();

		byte[]? encrypted = sut.Encrypt(keeperId, Identity, contents);

		sut.Decrypt(keeperId, Identity, encrypted!)
			.Should()
			.Equal(contents);
	}

	/// <summary>
	/// <see cref="SessionKeyStore.Unlock" />: a session secret made for a key that cannot be wrapped is wiped, as no
	/// key needs it.
	/// </summary>
	[Test]
	public void Unlock_Wipes_A_New_Session_Secret_When_The_Key_Cannot_Be_Wrapped()
	{
		// Arrange
		PinnedBuffer? sessionId = null;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ISessionDekWrapper sessionDekWrapper = Substitute.For<ISessionDekWrapper>();

			sessionDekWrapper
				.EncryptWithSessionId(Arg.Any<PinnedBuffer>(), Arg.Do<PinnedBuffer>(x => sessionId = x), Arg.Any<ContentIdentity>())
				.Throws(new CryptographicException());

			builder.RegisterInstance(sessionDekWrapper);
		});

		SessionKeyStore sut = mock.Create<SessionKeyStore>();

		// Act
		Action unlock = () => sut.Unlock(Guid.NewGuid(), SecretFactory.CreateRandomKey(DekSize));

		// Assert
		unlock
			.Should()
			.ThrowExactly<CryptographicException>();

		Action read = () => sessionId!.AsReadOnlySpan();

		read
			.Should()
			.ThrowExactly<ObjectDisposedException>();
	}
	#endregion
}
