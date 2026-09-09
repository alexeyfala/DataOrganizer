using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Messages;
using DataOrganizer.Services;
using Moq;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces;
using Shared.Extensions;
using SharpHook;
using SharpHook.Data;
using SharpHook.Testing;
using System;
using System.Linq;
using System.Threading.Tasks;
using TestSupport;

namespace DataOrganizer.UnitTests;

[TestFixture(Description = $@"Tests of ""{nameof(KeyboardInputHook)}"" type")]
internal class KeyboardInputHookTests
{
	#region Methods
	/// <summary>
	/// <see cref="KeyboardInputHook.Dispose" />: the files and input stack are cleared and the shared hook stays alive.
	/// </summary>
	[Test]
	public void Dispose_Clears_State_And_Keeps_Hook()
	{
		// Arrange
		TestGlobalHook hook = new();

		using AutoMock mock = AutoMock.GetLoose();

		GlobalHookRunner runner = mock.Create<GlobalHookRunner>(TypedParameter.From<IGlobalHook>(hook));

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>(TypedParameter.From<IGlobalHookRunner>(runner));

		sut
			.Files
			.AddRange(TestData.CreateFilesDto(5));

		sut
			.InputStack
			.AddRange(TestData.CreateKeyStrokes(5));

		// Act
		sut.Dispose();

		hook.IsDisposed
			.Should()
			.BeFalse();

		sut.Files
			.Should()
			.BeEmpty();

		sut.InputStack
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: protected contents are flagged sensitive (written via <see cref="IClipboardAccessor.SetDataAsync" />).
	/// </summary>
	[AvaloniaTest]
	public async Task HandleKeyReleasedAsync_Flags_Sensitive_When_Encrypted()
	{
		// Arrange
		FileDto dto = TestData.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		const KeyCode code = KeyCode.VcA;

		const EventMask mask = EventMask.LeftCtrl;

		KeyStroke[] keyStrokes = [.. Enumerable.Repeat(new KeyStroke()
		{
			Code = code,
			Mask = mask
		}, 5)];

		dto
			.Hotkeys
			.AddRange(keyStrokes.ToHotkeyDtos());

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(new ValidatedContents
				{
					Contents = TestData.CreateRandomBytes(10),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryToDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>())
				.Returns(TextDefaults.Encoding.GetBytes(SampleText.LoremIpsum));

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(clipboard);
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut
			.Files
			.Add(dto);

		sut
			.InputStack
			.AddRange(keyStrokes);

		// Act
		await sut.HandleKeyReleasedAsync(mask, code);

		// Assert
		await clipboard
			.Received(1)
			.SetDataAsync(Arg.Any<DataTransfer>());

		await clipboard
			.DidNotReceive()
			.SetTextAsync(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: a matching hotkey copies the decrypted contents to the clipboard and shows a toast.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Sets_Text_To_Clipboard()
	{
		// Arrange
		FileDto dto = TestData.CreateFileDto();

		const KeyCode code = KeyCode.VcA;

		const EventMask mask = EventMask.LeftCtrl;

		KeyStroke[] keyStrokes = [.. Enumerable.Repeat(new KeyStroke()
		{
			Code = code,
			Mask = mask
		}, 5)];

		dto
			.Hotkeys
			.AddRange(keyStrokes.ToHotkeyDtos());

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		INotificationService notificationService = Substitute.For<INotificationService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(new ValidatedContents
				{
					Contents = TextDefaults.Encoding.GetBytes(SampleText.LoremIpsum),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryToDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>())
				.Returns(TestData.CreateRandomBytes(10));

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(clipboard);

			builder.RegisterInstance(notificationService);
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut
			.Files
			.Add(dto);

		sut
			.InputStack
			.AddRange(keyStrokes);

		// Act
		await sut.HandleKeyReleasedAsync(mask, code);

		// Assert
		notificationService
			.Received()
			.ShowToast(Arg.Any<string>());

		await clipboard
			.Received()
			.SetTextAsync(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.Receive" />: a released key message is handed to the asynchronous handler.
	/// </summary>
	[Test]
	public void Receive_Hands_Message_To_Handler()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		Mock<ITaskExceptionHandler> exceptionHandler = mock.Mock<ITaskExceptionHandler>();

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		// Act
		sut.Receive(new GlobalKeyReleasedMessage(EventMask.LeftCtrl, KeyCode.VcA));

		// Assert
		exceptionHandler.Verify(x => x.Watch(It.IsAny<Task>()), Times.Once);
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.StopTrackingAsync" />: the running hook is stopped and the files and input stack are cleared.
	/// </summary>
	[Test]
	public async Task StopTrackingAsync_Stops_Hook()
	{
		// Arrange
		TestGlobalHook hook = new();

		using AutoMock mock = AutoMock.GetLoose();

		GlobalHookRunner runner = mock.Create<GlobalHookRunner>(TypedParameter.From<IGlobalHook>(hook));

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>(TypedParameter.From<IGlobalHookRunner>(runner));

		sut
			.Files
			.AddRange(TestData.CreateFilesDto(5));

		sut
			.InputStack
			.AddRange(TestData.CreateKeyStrokes(5));

		await runner.StartAsync();

		sut.IsRunning
			.Should()
			.BeTrue();

		// Act
		await sut.StopTrackingAsync();

		// Assert
		sut.IsRunning
			.Should()
			.BeFalse();

		sut.Files
			.Should()
			.BeEmpty();

		sut.InputStack
			.Should()
			.BeEmpty();
	}
	#endregion
}
