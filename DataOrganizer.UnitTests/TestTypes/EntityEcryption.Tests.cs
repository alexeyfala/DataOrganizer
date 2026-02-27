using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Shared.Common;
using Shared.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EntityEcryption)}"" type")]
internal class EntityEcryptionTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="EntityEcryption.DecryptSessionContents" />.
	/// </summary>
	[Test]
	public void DecryptSessionContents_Cannot_Decrypt_Binary()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			encryption.Decrypt(
				Arg.Any<byte[]>(),
				Arg.Any<byte[]>(),
				out Arg.Any<byte[]>()).Returns(true, false);

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		bool result = sut.DecryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10),
			out _);

		// Assert
		result
			.Should()
			.BeFalse();

		encryption.Received(2).Decrypt(
			Arg.Any<byte[]>(),
			Arg.Any<byte[]>(),
			out Arg.Any<byte[]>());
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.DecryptSessionContents" />.
	/// </summary>
	[Test]
	public void DecryptSessionContents_Cannot_Decrypt_Encrypted_Password()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			encryption.Decrypt(
				Arg.Any<byte[]>(),
				Arg.Any<byte[]>(),
				out Arg.Any<byte[]>()).Returns(false);

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		bool result = sut.DecryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10),
			out _);

		// Assert
		result
			.Should()
			.BeFalse();

		encryption.Received(1).Decrypt(
			Arg.Any<byte[]>(),
			Arg.Any<byte[]>(),
			out Arg.Any<byte[]>());
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.DecryptSessionContents" />.
	/// </summary>
	[Test]
	public void DecryptSessionContents_Does_Work()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			encryption.Decrypt(
				Arg.Any<byte[]>(),
				Arg.Any<byte[]>(),
				out Arg.Any<byte[]>()).Returns(_ => true, x =>
				{
					x[2] = TestUtils.CreateRandomBytes(10);

					return true;
				});

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		bool result = sut.DecryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10),
			out byte[] output);

		// Assert
		result
			.Should()
			.BeTrue();

		output
			.Should()
			.NotBeEmpty();
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptSessionContents" />.
	/// </summary>
	[Test]
	public void EncryptSessionContents_Cannot_Decrypt_Encrypted_Password()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			encryption.Decrypt(
				Arg.Any<byte[]>(),
				Arg.Any<byte[]>(),
				out Arg.Any<byte[]>()).Returns(false);

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		bool result = sut.EncryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10),
			out _);

		// Assert
		result
			.Should()
			.BeFalse();

		encryption.Received(0).Encrypt(
			Arg.Any<byte[]>(),
			Arg.Any<byte[]>(),
			out Arg.Any<byte[]>());
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptSessionContents" />.
	/// </summary>
	[Test]
	public void EncryptSessionContents_Cannot_Encrypt_Binary()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			encryption.Decrypt(
				Arg.Any<byte[]>(),
				Arg.Any<byte[]>(),
				out Arg.Any<byte[]>()).Returns(true);

			encryption.Encrypt(
				Arg.Any<byte[]>(),
				Arg.Any<byte[]>(),
				out Arg.Any<byte[]>()).Returns(false);

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		bool result = sut.EncryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10),
			out _);

		// Assert
		result
			.Should()
			.BeFalse();

		encryption.Received().Encrypt(
			Arg.Any<byte[]>(),
			Arg.Any<byte[]>(),
			out Arg.Any<byte[]>());

		encryption.Received().Decrypt(
			Arg.Any<byte[]>(),
			Arg.Any<byte[]>(),
			out Arg.Any<byte[]>());
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.EncryptSessionContents" />.
	/// </summary>
	[Test]
	public void EncryptSessionContents_Does_Work()
	{
		// Arrange
		IEncryptionService encryption = Substitute.For<IEncryptionService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			encryption.Decrypt(
				Arg.Any<byte[]>(),
				Arg.Any<byte[]>(),
				out Arg.Any<byte[]>()).Returns(true);

			encryption.Encrypt(
				Arg.Any<byte[]>(),
				Arg.Any<byte[]>(),
				out Arg.Any<byte[]>()).Returns(x =>
				{
					x[2] = TestUtils.CreateRandomBytes(10);

					return true;
				});

			builder.RegisterInstance(encryption);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		bool result = sut.EncryptSessionContents(
			TestUtils.CreateRandomBytes(10),
			TestUtils.CreateRandomBytes(10),
			out byte[] output);

		// Assert
		result
			.Should()
			.BeTrue();

		output
			.Should()
			.NotBeEmpty();
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.GetSessionId" />.
	/// </summary>
	[Test]
	public void GetSessionId_Returns_Different_Values_Between_Sessions()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		byte[] first = sut.GetSessionId();

		sut.ResetSessionId();

		byte[] second = sut.GetSessionId();

		// Assert
		first
			.Should()
			.NotBeEquivalentTo(second);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.GetSessionId" />.
	/// </summary>
	[Test]
	public void GetSessionId_Returns_Same_Value_During_Session()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		byte[] value = sut.GetSessionId();

		// Assert
		for (int i = 0; i < 10; i++)
		{
			sut.GetSessionId()
				.Should()
				.BeEquivalentTo(value);
		}
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.HideFileContentsAsync" />.
	/// </summary>
	[AvaloniaTest]
	public async Task HideFileContentsAsync_Does_Work([Values] bool isEdited)
	{
		// Arrange
		FileModelDto file = isEdited
			? TestUtils.CreateFileDto(isEdited: true)
			: TestUtils.CreateFileDto(isExecuted: true);

		file.EncryptionStatus = EncryptionStatus.Decrypted;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock mock = AutoMock.GetLoose();

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateUserControl<EditFilesView>()
				.Returns(mock.Create<EditFilesView>());

			YesNoCancelBox view = mock.Create<YesNoCancelBox>();

			viewFactory
				.CreateUserControl<YesNoCancelBox>()
				.Returns(view);

			builder.RegisterInstance(viewFactory);

			_ = view
				.ViewModel
				.SetResultAsync(YesNoCancelResult.Yes);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		await sut.HideFileContentsAsync(file, mock.Create<EditorViewModel>());

		// Assert
		file.IsEdited
			.Should()
			.BeFalse();

		file.IsExecuted
			.Should()
			.BeFalse();

		file.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Encrypted);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.HideFolderContents" />.
	/// </summary>
	[AvaloniaTest]
	public void HideFolderContents_Does_Work()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		FileModelDto[] editedFiles = [.. TestUtils.CreateFilesDto(5, isEdited: true)];

		FileModelDto[] executedFiles = [.. TestUtils.CreateFilesDto(5, isExecuted: true)];

		folder
			.Children
			.AddRange(editedFiles.Concat(executedFiles));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock mock = AutoMock.GetLoose();

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateUserControl<EditFilesView>()
				.Returns(mock.Create<EditFilesView>());

			YesNoCancelBox view = mock.Create<YesNoCancelBox>();

			viewFactory
				.CreateUserControl<YesNoCancelBox>()
				.Returns(view);

			builder.RegisterInstance(viewFactory);

			_ = view
				.ViewModel
				.SetResultAsync(YesNoCancelResult.Yes);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		sut.HideFolderContents(folder, mock.Create<EditorViewModel>());

		// Assert
		folder.SessionEncryptedDek
			.Should()
			.BeNull();

		folder.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Encrypted);

		folder.GetAllChildren()
			.Should()
			.OnlyContain(x => x.EncryptionStatus == EncryptionStatus.Encrypted);

		editedFiles
			.Should()
			.OnlyContain(x => !x.IsEdited);

		executedFiles
			.Should()
			.OnlyContain(x => !x.IsExecuted);
	}

	/// <summary>
	/// Test of <see cref="EntityEcryption.ShowFileContentsAsync" />.
	/// </summary>
	[AvaloniaTest]
	public async Task ShowFileContentsAsync_Does_Work()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.PasswordHash = AppUtils.CreateRandomString(10);

		folder.EncryptedDek = TestUtils.CreateRandomBytes(10);

		FileModelDto file = TestUtils.CreateFileDto();

		folder
			.Children
			.Add(file);

		file.Parent = folder;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			using AutoMock mock = AutoMock.GetLoose();

			PasswordBox view = mock.Create<PasswordBox>();

			view
				.ViewModel
				.Password = AppUtils.CreateRandomString(10);

			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateUserControl<PasswordBox>()
				.Returns(view);

			IEncryptionService encryption = Substitute.For<IEncryptionService>();

			encryption
				.EnhancedVerify(Arg.Any<string>(), Arg.Any<string>())
				.Returns(true);

			encryption
				.Decrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), out _)
				.Returns(x =>
				{
					x[2] = TestUtils.CreateRandomBytes(10);

					return true;
				});

			encryption
				.Encrypt(Arg.Any<byte[]>(), Arg.Any<byte[]>(), out _)
				.Returns(x =>
				{
					x[2] = TestUtils.CreateRandomBytes(10);

					return true;
				});

			builder.RegisterInstance(viewFactory);

			builder.RegisterInstance(encryption);

			_ = view
				.ViewModel
				.SetResultAsync(true);
		});

		EntityEcryption sut = mock.Create<EntityEcryption>();

		// Act
		await sut.ShowFileContentsAsync(file, mock.Create<EditorViewModel>());

		// Assert
		folder.SessionEncryptedDek
			.Should()
			.NotBeEmpty();

		file.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Decrypted);
	}
	#endregion
}
