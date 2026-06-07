using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using NSubstitute;
using Repository.DTO;
using Repository.Interfaces;
using Shared.Extensions;
using SharpHook;
using SharpHook.Data;
using SharpHook.Testing;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(KeyboardInputHook)}"" type")]
internal class KeyboardInputHookTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="KeyboardInputHook.Dispose" />: the underlying hook is disposed and the files and input stack are cleared.
	/// </summary>
	[Test]
	public void Dispose_Disposes_Hook()
	{
		// Arrange
		TestGlobalHook hook = new();

		using AutoMock mock = AutoMock.GetLoose();

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>(TypedParameter.From<IGlobalHook>(hook));

		sut
			.Files
			.AddRange(TestUtils.CreateFilesDto(5));

		sut
			.InputStack
			.AddRange(TestUtils.CreateCodeMaskPairs(5));

		// Act
		sut.Dispose();

		// Assert
		hook.IsDisposed
			.Should()
			.BeTrue();

		sut.Files
			.Should()
			.BeEmpty();

		sut.InputStack
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Test of <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: a matching hotkey copies the decrypted contents to the clipboard and shows a toast.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Sets_Text_To_Clipboard()
	{
		// Arrange
		FileModelDto dto = TestUtils.CreateFileDto();

		const KeyCode code = KeyCode.VcA;

		const EventMask mask = EventMask.LeftCtrl;

		CodeMaskPair[] pairs = [.. Enumerable.Repeat(new CodeMaskPair()
		{
			Code = code,
			Mask = mask
		}, 5)];

		dto
			.Hotkeys
			.AddRange(pairs.ToHotkeyModelsDto());

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		INotificationService notificationService = Substitute.For<INotificationService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(new ContentsIsValidPair
				{
					Contents = TextHelper.Utf8Encoding.GetBytes(TextHelper.LoremIpsum),
					IsValid = true
				});

			IEntityEncryption entityEncryption = Substitute.For<IEntityEncryption>();

			entityEncryption
				.TryToDecryptContentsAsync(Arg.Any<FileModelDto>(), Arg.Any<byte[]>(), Arg.Any<string>())
				.Returns(TestUtils.CreateRandomBytes(10));

			builder.RegisterInstance(entityEncryption);

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
			.AddRange(pairs);

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
	/// Test of <see cref="KeyboardInputHook.StopTrackingAsync" />: the running hook is stopped and the files and input stack are cleared.
	/// </summary>
	[Test]
	public async Task StopTrackingAsync_Stops_Hook()
	{
		// Arrange
		TestGlobalHook hook = new();

		using AutoMock mock = AutoMock.GetLoose();

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>(TypedParameter.From<IGlobalHook>(hook));

		sut
			.Files
			.AddRange(TestUtils.CreateFilesDto(5));

		sut
			.InputStack
			.AddRange(TestUtils.CreateCodeMaskPairs(5));

		_ = hook.RunAsync();

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
