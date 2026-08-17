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
	/// <see cref="EmbeddedEditorViewModelBase.TryToDecrypt" />: delegates to the cipher when a keeper is known.
	/// </summary>
	[Test]
	public void TryToDecrypt_Delegates_To_Store_When_Keeper_Is_Known()
	{
		// Arrange
		byte[] input = [1, 2, 3];

		byte[] decrypted = [7];

		Guid keeperId = Guid.NewGuid();

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		contentCipher
			.TryDecrypt(keeperId, Arg.Any<ContentIdentity>(), input)
			.Returns(decrypted);

		TestEditor sut = new(contentCipher)
		{
			KeeperId = keeperId
		};

		// Act
		byte[]? result = sut.InvokeTryToDecrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(decrypted);

		contentCipher
			.Received(1)
			.TryDecrypt(keeperId, Arg.Any<ContentIdentity>(), input);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToDecrypt" />: returns the input unchanged when it is empty.
	/// </summary>
	[Test]
	public void TryToDecrypt_Returns_Input_When_Input_Is_Empty()
	{
		// Arrange
		byte[] input = [];

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		TestEditor sut = new(contentCipher)
		{
			KeeperId = Guid.NewGuid()
		};

		// Act
		byte[]? result = sut.InvokeTryToDecrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(input);

		contentCipher
			.DidNotReceive()
			.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToDecrypt" />: returns the input unchanged when no keeper is known.
	/// </summary>
	[Test]
	public void TryToDecrypt_Returns_Input_When_No_Keeper()
	{
		// Arrange
		byte[] input = [1, 2, 3];

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		TestEditor sut = new(contentCipher);

		// Act
		byte[]? result = sut.InvokeTryToDecrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(input);

		contentCipher
			.DidNotReceive()
			.TryDecrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToEncrypt" />: delegates to the cipher when a keeper is known.
	/// </summary>
	[Test]
	public void TryToEncrypt_Delegates_To_Store_When_Keeper_Is_Known()
	{
		// Arrange
		byte[] input = [1, 2, 3];

		byte[] encrypted = [7];

		Guid keeperId = Guid.NewGuid();

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		contentCipher
			.TryEncrypt(keeperId, Arg.Any<ContentIdentity>(), input)
			.Returns(encrypted);

		TestEditor sut = new(contentCipher)
		{
			KeeperId = keeperId
		};

		// Act
		byte[]? result = sut.InvokeTryToEncrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(encrypted);

		contentCipher
			.Received(1)
			.TryEncrypt(keeperId, Arg.Any<ContentIdentity>(), input);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToEncrypt" />: returns the input unchanged when it is empty.
	/// </summary>
	[Test]
	public void TryToEncrypt_Returns_Input_When_Input_Is_Empty()
	{
		// Arrange
		byte[] input = [];

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		TestEditor sut = new(contentCipher)
		{
			KeeperId = Guid.NewGuid()
		};

		// Act
		byte[]? result = sut.InvokeTryToEncrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(input);

		contentCipher
			.DidNotReceive()
			.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToEncrypt" />: returns the input unchanged when no keeper is known.
	/// </summary>
	[Test]
	public void TryToEncrypt_Returns_Input_When_No_Keeper()
	{
		// Arrange
		byte[] input = [1, 2, 3];

		IContentCipher contentCipher = Substitute.For<IContentCipher>();

		TestEditor sut = new(contentCipher);

		// Act
		byte[]? result = sut.InvokeTryToEncrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(input);

		contentCipher
			.DidNotReceive()
			.TryEncrypt(Arg.Any<Guid>(), Arg.Any<ContentIdentity>(), Arg.Any<byte[]>());
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
	public TestEditor(IContentCipher contentCipher) : base(
		null!,
		contentCipher,
		null!,
		null!,
		null!,
		Substitute.For<IMessenger>(),
		null!)
	{
	}

	public byte[]? InvokeTryToDecrypt(byte[] input) => TryToDecrypt(input);

	public byte[]? InvokeTryToEncrypt(byte[] input) => TryToEncrypt(input);
}
