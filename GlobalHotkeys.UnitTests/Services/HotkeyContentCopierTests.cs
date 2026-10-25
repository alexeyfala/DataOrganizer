using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Clipboard.Interfaces;
using CommunityToolkit.Mvvm.Messaging;
using Favorites.Messages;
using FileContents.Enums;
using GlobalHotkeys.Services;
using GlobalHotkeys.UnitTests.Factories;
using GlobalHotkeys.UnitTests.Fakes;
using Hierarchy.Dto;
using Hierarchy.Enums;
using NSubstitute;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Threading.Interfaces;
using WindowContracts.Interfaces;

namespace GlobalHotkeys.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(HotkeyContentCopier)}"" type")]
internal class HotkeyContentCopierTests
{
	#region Methods
	/// <summary>
	/// <see cref="HotkeyContentCopier.CopyAsync" />: the main window comes back with its favorites before the copy of an
	/// encrypted file, for the password of its keeper.
	/// </summary>
	[Test]
	public async Task CopyAsync_Brings_Back_The_Main_Window_For_An_Encrypted_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		List<string> steps = [];

		IMessenger messenger = new StrongReferenceMessenger();

		messenger.Register<ShowFavoritesMessage>(this, (_, _) => steps.Add("favorites"));

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMainWindowActivator mainWindowActivator = Substitute.For<IMainWindowActivator>();

			mainWindowActivator
				.When(x => x.ActivateMainWindow())
				.Do(_ => steps.Add("activate"));

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(mainWindowActivator);

			builder.RegisterInstance(messenger);
		});

		HotkeyContentCopier sut = mock.Create<HotkeyContentCopier>();

		// Act
		await sut.CopyAsync(file);

		// Assert
		steps
			.Should()
			.Equal("activate", "favorites");
	}

	/// <summary>
	/// <see cref="HotkeyContentCopier.CopyAsync" />: a copy that does not reach the clipboard leaves the copy history
	/// alone.
	/// </summary>
	[TestCase(FileTextStatus.Closed)]
	[TestCase(FileTextStatus.Damaged)]
	[TestCase(FileTextStatus.Empty)]
	[TestCase(FileTextStatus.NotText)]
	public async Task CopyAsync_Keeps_The_Copy_History_When_Nothing_Is_Copied(FileTextStatus status)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		ICopyHistoryRecorder copyHistory = Substitute.For<ICopyHistoryRecorder>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileTextCopier fileTextCopier = Substitute.For<IFileTextCopier>();

			fileTextCopier
				.CopyAsync(file, Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(status);

			builder.RegisterInstance(copyHistory);

			builder.RegisterInstance(fileTextCopier);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		HotkeyContentCopier sut = mock.Create<HotkeyContentCopier>();

		// Act
		await sut.CopyAsync(file);

		// Assert
		copyHistory
			.DidNotReceive()
			.Add(Arg.Any<FileDto>(), Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="HotkeyContentCopier.CopyAsync" />: the main window is left alone for a file that is not encrypted.
	/// </summary>
	[Test]
	public async Task CopyAsync_Leaves_The_Main_Window_Alone_For_A_File_That_Is_Not_Encrypted()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		IMainWindowActivator mainWindowActivator = Substitute.For<IMainWindowActivator>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(mainWindowActivator);
		});

		HotkeyContentCopier sut = mock.Create<HotkeyContentCopier>();

		// Act
		await sut.CopyAsync(file);

		// Assert
		mainWindowActivator
			.DidNotReceive()
			.ActivateMainWindow();
	}

	/// <summary>
	/// <see cref="HotkeyContentCopier.CopyAsync" />: a copied file goes to the top of the copy history at once.
	/// </summary>
	[Test]
	public async Task CopyAsync_Puts_The_File_Into_The_Copy_History()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		ICopyHistoryRecorder copyHistory = Substitute.For<ICopyHistoryRecorder>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileTextCopier fileTextCopier = Substitute.For<IFileTextCopier>();

			fileTextCopier
				.CopyAsync(file, Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(FileTextStatus.Text);

			builder.RegisterInstance(copyHistory);

			builder.RegisterInstance(fileTextCopier);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		HotkeyContentCopier sut = mock.Create<HotkeyContentCopier>();

		// Act
		await sut.CopyAsync(file);

		// Assert
		copyHistory
			.Received(1)
			.Add(file, true);
	}
	#endregion
}
