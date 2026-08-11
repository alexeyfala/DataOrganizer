using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.ViewModels;
using NSubstitute;
using System;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EmbeddedEditorViewModelBase)}"" type")]
internal class EmbeddedEditorViewModelBaseTests
{
	#region Methods
	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToDecrypt" />: delegates to the key store when a keeper is known.
	/// </summary>
	[Test]
	public void TryToDecrypt_Delegates_To_Store_When_Keeper_Is_Known()
	{
		// Arrange
		byte[] input = [1, 2, 3];

		byte[] decrypted = [7];

		Guid keeperId = Guid.NewGuid();

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		sessionKeyStore
			.Decrypt(keeperId, Arg.Any<ContentIdentity>(), input)
			.Returns(decrypted);

		TestEditor sut = new(sessionKeyStore)
		{
			KeeperId = keeperId
		};

		// Act
		byte[]? result = sut.InvokeTryToDecrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(decrypted);

		sessionKeyStore
			.Received(1)
			.Decrypt(keeperId, Arg.Any<ContentIdentity>(), input);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToDecrypt" />: returns the input unchanged when it is empty.
	/// </summary>
	[Test]
	public void TryToDecrypt_Returns_Input_When_Input_Is_Empty()
	{
		// Arrange
		byte[] input = [];

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		TestEditor sut = new(sessionKeyStore)
		{
			KeeperId = Guid.NewGuid()
		};

		// Act
		byte[]? result = sut.InvokeTryToDecrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(input);

		sessionKeyStore
			.DidNotReceive()
			.Decrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToDecrypt" />: returns the input unchanged when no keeper is known.
	/// </summary>
	[Test]
	public void TryToDecrypt_Returns_Input_When_No_Keeper()
	{
		// Arrange
		byte[] input = [1, 2, 3];

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		TestEditor sut = new(sessionKeyStore);

		// Act
		byte[]? result = sut.InvokeTryToDecrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(input);

		sessionKeyStore
			.DidNotReceive()
			.Decrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToEncrypt" />: delegates to the key store when a keeper is known.
	/// </summary>
	[Test]
	public void TryToEncrypt_Delegates_To_Store_When_Keeper_Is_Known()
	{
		// Arrange
		byte[] input = [1, 2, 3];

		byte[] encrypted = [7];

		Guid keeperId = Guid.NewGuid();

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		sessionKeyStore
			.Encrypt(keeperId, Arg.Any<ContentIdentity>(), input)
			.Returns(encrypted);

		TestEditor sut = new(sessionKeyStore)
		{
			KeeperId = keeperId
		};

		// Act
		byte[]? result = sut.InvokeTryToEncrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(encrypted);

		sessionKeyStore
			.Received(1)
			.Encrypt(keeperId, Arg.Any<ContentIdentity>(), input);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToEncrypt" />: returns the input unchanged when it is empty.
	/// </summary>
	[Test]
	public void TryToEncrypt_Returns_Input_When_Input_Is_Empty()
	{
		// Arrange
		byte[] input = [];

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		TestEditor sut = new(sessionKeyStore)
		{
			KeeperId = Guid.NewGuid()
		};

		// Act
		byte[]? result = sut.InvokeTryToEncrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(input);

		sessionKeyStore
			.DidNotReceive()
			.Encrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToEncrypt" />: returns the input unchanged when no keeper is known.
	/// </summary>
	[Test]
	public void TryToEncrypt_Returns_Input_When_No_Keeper()
	{
		// Arrange
		byte[] input = [1, 2, 3];

		ISessionKeyStore sessionKeyStore = Substitute.For<ISessionKeyStore>();

		TestEditor sut = new(sessionKeyStore);

		// Act
		byte[]? result = sut.InvokeTryToEncrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(input);

		sessionKeyStore
			.DidNotReceive()
			.Encrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());
	}
	#endregion
}

// TestEditor is a top-level internal type so the CommunityToolkit messenger source generator
// (IMessengerRegisterAllGenerator), which emits a RegisterAll for every IRecipient<T> recipient,
// can access it — a private nested type would be inaccessible to the generated code.

/// <summary>
/// Minimal concrete editor exposing the protected encryption helpers; unused base dependencies are left null.
/// </summary>
internal sealed class TestEditor : EmbeddedEditorViewModelBase
{
	public TestEditor(ISessionKeyStore sessionKeyStore) : base(
		null!,
		null!,
		null!,
		null!,
		Substitute.For<IMessenger>(),
		sessionKeyStore,
		null!)
	{
	}

	public byte[]? InvokeTryToDecrypt(byte[] input) => TryToDecrypt(input);

	public byte[]? InvokeTryToEncrypt(byte[] input) => TryToEncrypt(input);
}
