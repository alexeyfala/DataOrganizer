using Autofac;
using Autofac.Extras.Moq;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Services.Encryption;
using DataOrganizer.UnitTests.Factories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Encryption;

[TestFixture(Description = $@"Tests of ""{nameof(FolderCipher)}"" type")]
internal class FolderCipherTests
{
	#region Methods
	/// <summary>
	/// <see cref="FolderCipher.ChangePasswordAsync" />: a cryptographic failure goes to the reporter of encryption failures,
	/// and any other to the reporter of database failures; the log names the operation.
	/// </summary>
	[Test]
	public async Task ChangePasswordAsync_Tells_A_Cryptographic_Failure_From_Another([Values] bool isCryptographic)
	{
		// Arrange
		Exception failure = isCryptographic
			? new CryptographicException()
			: new InvalidOperationException();

		IDbFailureReporter dbFailureReporter = Substitute.For<IDbFailureReporter>();

		IEncryptionFailureReporter encryptionFailureReporter = Substitute.For<IEncryptionFailureReporter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPasswordDekWrapper encryption = Substitute.For<IPasswordDekWrapper>();

			encryption
				.Encrypt(default!, default!, default)
				.ThrowsForAnyArgs(failure);

			builder.RegisterInstance(dbFailureReporter);

			builder.RegisterInstance(encryption);

			builder.RegisterInstance(encryptionFailureReporter);
		});

		FolderCipher sut = mock.Create<FolderCipher>();

		using PinnedBuffer dek = SecretFactory.CreateRandomKey();

		using PinnedSecret password = new(length: 8);

		// Act
		await sut.ChangePasswordAsync(ItemDtoFactory.CreateFolderDto(), dek, password);

		// Assert
		encryptionFailureReporter
			.Received(isCryptographic ? 1 : 0)
			.Report(failure, nameof(FolderCipher.ChangePasswordAsync));

		dbFailureReporter
			.Received(isCryptographic ? 0 : 1)
			.Report(failure, Arg.Any<string>());
	}
	#endregion
}
