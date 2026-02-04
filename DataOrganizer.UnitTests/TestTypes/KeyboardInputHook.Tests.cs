using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Input.Platform;
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
	/// Test of <see cref="KeyboardInputHook.Dispose" />.
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
	/// Test of <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />.
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

		IClipboard clipboard = Substitute.For<IClipboard>();

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

			IClipboardService clipboardService = Substitute.For<IClipboardService>();

			clipboardService
				.FindClipboard()
				.Returns(clipboard);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(clipboardService);

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
	/// Test of <see cref="KeyboardInputHook.StopTracking" />.
	/// </summary>
	[Test]
	public void StopTracking_Stops_Hook()
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
		sut.StopTracking();

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
