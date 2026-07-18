using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.ViewModels;
using NSubstitute;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EmbeddedEditorViewModelBase)}"" type")]
internal class EmbeddedEditorViewModelBaseTests
{
	#region Methods
	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToDecrypt" />: delegates to the encryption service when a session key is present.
	/// </summary>
	[Test]
	public void TryToDecrypt_Delegates_To_Encryption_When_Session_Key_Present()
	{
		// Arrange
		byte[] input = [1, 2, 3];

		byte[] dek = [9, 9];

		byte[] decrypted = [7];

		IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

		encryption
			.DecryptSessionContents(input, dek)
			.Returns(decrypted);

		TestEditor sut = new(encryption)
		{
			SessionEncryptedDek = dek
		};

		// Act
		byte[]? result = sut.InvokeTryToDecrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(decrypted);

		encryption
			.Received(1)
			.DecryptSessionContents(input, dek);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToDecrypt" />: returns the input unchanged when it is empty.
	/// </summary>
	[Test]
	public void TryToDecrypt_Returns_Input_When_Input_Is_Empty()
	{
		// Arrange
		byte[] input = [];

		IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

		TestEditor sut = new(encryption)
		{
			SessionEncryptedDek = [9, 9]
		};

		// Act
		byte[]? result = sut.InvokeTryToDecrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(input);

		encryption
			.DidNotReceive()
			.DecryptSessionContents(Arg.Any<byte[]>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToDecrypt" />: returns the input unchanged when no session key is present.
	/// </summary>
	[Test]
	public void TryToDecrypt_Returns_Input_When_No_Session_Key()
	{
		// Arrange
		byte[] input = [1, 2, 3];

		IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

		TestEditor sut = new(encryption);

		// Act
		byte[]? result = sut.InvokeTryToDecrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(input);

		encryption
			.DidNotReceive()
			.DecryptSessionContents(Arg.Any<byte[]>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToEncrypt" />: delegates to the encryption service when a session key is present.
	/// </summary>
	[Test]
	public void TryToEncrypt_Delegates_To_Encryption_When_Session_Key_Present()
	{
		// Arrange
		byte[] input = [1, 2, 3];

		byte[] dek = [9, 9];

		byte[] encrypted = [7];

		IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

		encryption
			.EncryptSessionContents(input, dek)
			.Returns(encrypted);

		TestEditor sut = new(encryption)
		{
			SessionEncryptedDek = dek
		};

		// Act
		byte[]? result = sut.InvokeTryToEncrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(encrypted);

		encryption
			.Received(1)
			.EncryptSessionContents(input, dek);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToEncrypt" />: returns the input unchanged when it is empty.
	/// </summary>
	[Test]
	public void TryToEncrypt_Returns_Input_When_Input_Is_Empty()
	{
		// Arrange
		byte[] input = [];

		IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

		TestEditor sut = new(encryption)
		{
			SessionEncryptedDek = [9, 9]
		};

		// Act
		byte[]? result = sut.InvokeTryToEncrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(input);

		encryption
			.DidNotReceive()
			.EncryptSessionContents(Arg.Any<byte[]>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.TryToEncrypt" />: returns the input unchanged when no session key is present.
	/// </summary>
	[Test]
	public void TryToEncrypt_Returns_Input_When_No_Session_Key()
	{
		// Arrange
		byte[] input = [1, 2, 3];

		IEntityEncryption encryption = Substitute.For<IEntityEncryption>();

		TestEditor sut = new(encryption);

		// Act
		byte[]? result = sut.InvokeTryToEncrypt(input);

		// Assert
		result
			.Should()
			.BeSameAs(input);

		encryption
			.DidNotReceive()
			.EncryptSessionContents(Arg.Any<byte[]>(), Arg.Any<byte[]>());
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
	public TestEditor(IEntityEncryption entityEncryption)
		: base(null!, null!, entityEncryption, null!, null!, Substitute.For<IMessenger>(), null!)
	{
	}

	public byte[]? InvokeTryToDecrypt(byte[] input) => TryToDecrypt(input);

	public byte[]? InvokeTryToEncrypt(byte[] input) => TryToEncrypt(input);
}
