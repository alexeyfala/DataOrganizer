using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Messages.Hotkeys;
using DataOrganizer.Services.Hotkeys;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Services;
using SharpHook;
using SharpHook.Data;
using SharpHook.Testing;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestSupport.Common;
using TestSupport.Dto;

namespace DataOrganizer.UnitTests.Services.Hotkeys;

[TestFixture(Description = $@"Tests of ""{nameof(KeyboardInputHook)}"" type")]
internal class KeyboardInputHookTests
{
	#region Methods
	/// <summary>
	/// <see cref="KeyboardInputHook.Dispose" />: the tracked hierarchy and input stack are cleared and the shared hook stays alive.
	/// </summary>
	[Test]
	public void Dispose_Clears_State_And_Keeps_Hook()
	{
		// Arrange
		TestGlobalHook hook = new();

		using AutoMock mock = AutoMock.GetLoose();

		GlobalHookRunner runner = mock.Create<GlobalHookRunner>(TypedParameter.From<IGlobalHook>(hook));

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>(TypedParameter.From<IGlobalHookRunner>(runner));

		sut.Hierarchy = ItemDtoFactory.CreateFileDtos(5);

		sut
			.InputStack
			.AddRange(KeyStrokeFactory.CreateKeyStrokes(5));

		// Act
		sut.Dispose();

		hook.IsDisposed
			.Should()
			.BeFalse();

		sut.Hierarchy
			.Should()
			.BeNull();

		sut.InputStack
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: the keys of a hotkey that fired do not start the next one.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Clears_The_Input_After_A_Hotkey_Fires()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		KeyStroke[] hotkey =
		[
			new()
			{
				Code = KeyCode.VcA,
				Mask = EventMask.LeftCtrl
			}
		];

		dto
			.Hotkeys
			.AddRange(hotkey.ToHotkeyDtos());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(10),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>())
				.Returns(TextDefaults.Encoding.GetBytes(SampleText.LoremIpsum));

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		// Act
		await sut.HandleKeyReleasedAsync(EventMask.LeftCtrl, KeyCode.VcA);

		// Assert
		sut.InputStack
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: a key typed without a modifier ends the hotkey being typed,
	/// whichever lock is on.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Clears_The_Input_On_A_Key_Without_Modifiers(
		[Values(EventMask.None, EventMask.CapsLock, EventMask.NumLock, EventMask.ScrollLock)] EventMask lockState)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [ItemDtoFactory.CreateFileDto()];

		sut
			.InputStack
			.AddRange(KeyStrokeFactory.CreateKeyStrokes(3));

		// Act
		await sut.HandleKeyReleasedAsync(lockState, KeyCode.VcK);

		// Assert
		sut.InputStack
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: a hotkey of a file whose contents are not text puts nothing
	/// in the clipboard.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Copies_Nothing_That_Is_Not_Text()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		const KeyCode code = KeyCode.VcA;

		const EventMask mask = EventMask.LeftCtrl;

		KeyStroke[] keyStrokes =
		[
			new()
			{
				Code = code,
				Mask = mask
			}
		];

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
					Contents = RandomValues.CreateBytes(10),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			// UTF-16 without a byte order mark
			byte[] contents = [0x48, 0x00, 0x69, 0x00];

			contentCipher
				.TryDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>())
				.Returns(contents);

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		// Act
		await sut.HandleKeyReleasedAsync(mask, code);

		// Assert
		await clipboard
			.DidNotReceive()
			.SetTextAsync(Arg.Any<string>());

		await clipboard
			.DidNotReceive()
			.SetDataAsync(Arg.Any<DataTransfer>());
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: a hotkey copies the text in the encoding of the file, without
	/// its byte order mark.
	/// </summary>
	[TestCase(new byte[] { 0xEF, 0xBB, 0xBF, 0x48, 0x69 })]
	[TestCase(new byte[] { 0xFF, 0xFE, 0x48, 0x00, 0x69, 0x00 })]
	public async Task HandleKeyReleasedAsync_Copies_The_Text_In_Its_Encoding(byte[] contents)
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		const KeyCode code = KeyCode.VcA;

		const EventMask mask = EventMask.LeftCtrl;

		KeyStroke[] keyStrokes =
		[
			new()
			{
				Code = code,
				Mask = mask
			}
		];

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
					Contents = RandomValues.CreateBytes(10),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>())
				.Returns(contents);

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		// Act
		await sut.HandleKeyReleasedAsync(mask, code);

		// Assert
		await clipboard
			.Received(1)
			.SetTextAsync("Hi");
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: a hotkey copies the text in the encoding chosen for the file
	/// rather than in the one found from its contents.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Copies_The_Text_In_The_Chosen_Encoding()
	{
		// Arrange
		const string text = "Привет, мир";

		FileDto dto = ItemDtoFactory.CreateFileDto(editorState: new SystemTextJsonSerializer().Serialize(new FileEditorState
		{
			Encoding = "cp866"
		}));

		const KeyCode code = KeyCode.VcA;

		const EventMask mask = EventMask.LeftCtrl;

		KeyStroke[] keyStrokes =
		[
			new()
			{
				Code = code,
				Mask = mask
			}
		];

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
					Contents = RandomValues.CreateBytes(10),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>())
				.Returns(CodePagesEncodingProvider.Instance.GetEncoding(866)!.GetBytes(text));

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		// Act
		await sut.HandleKeyReleasedAsync(mask, code);

		// Assert
		await clipboard
			.Received(1)
			.SetTextAsync(text);
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: the lock states count neither in the typed keys nor in the
	/// stored hotkey.
	/// </summary>
	[TestCase(EventMask.LeftCtrl, EventMask.LeftCtrl | EventMask.CapsLock)]
	[TestCase(EventMask.LeftCtrl | EventMask.CapsLock, EventMask.LeftCtrl)]
	public async Task HandleKeyReleasedAsync_Fires_A_Hotkey_Whatever_The_Lock_State(EventMask storedMask, EventMask typedMask)
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		KeyStroke[] hotkey =
		[
			new()
			{
				Code = KeyCode.VcA,
				Mask = storedMask
			}
		];

		dto
			.Hotkeys
			.AddRange(hotkey.ToHotkeyDtos());

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(new ValidatedContents
				{
					Contents = RandomValues.CreateBytes(10),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>())
				.Returns(TextDefaults.Encoding.GetBytes(SampleText.LoremIpsum));

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		// Act
		await sut.HandleKeyReleasedAsync(typedMask, KeyCode.VcA);

		// Assert
		await clipboard
			.Received(1)
			.SetTextAsync(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: a file added to the tracked hierarchy fires its hotkey at once.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Fires_The_Hotkey_Of_A_File_Added_During_Tracking()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		KeyStroke[] hotkey =
		[
			new()
			{
				Code = KeyCode.VcA,
				Mask = EventMask.LeftCtrl
			}
		];

		dto
			.Hotkeys
			.AddRange(hotkey.ToHotkeyDtos());

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		dbAccess
			.GetFileContentsAsync(Arg.Any<Guid>())
			.Returns(new ValidatedContents());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = hierarchy;

		hierarchy.Add(dto);

		// Act
		await sut.HandleKeyReleasedAsync(EventMask.LeftCtrl, KeyCode.VcA);

		// Assert
		await dbAccess
			.Received(1)
			.GetFileContentsAsync(dto.Id);
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: protected contents are flagged sensitive (written via <see cref="IClipboardAccessor.SetDataAsync" />).
	/// </summary>
	[AvaloniaTest]
	public async Task HandleKeyReleasedAsync_Flags_Sensitive_When_Encrypted()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

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
					Contents = RandomValues.CreateBytes(10),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>())
				.Returns(TextDefaults.Encoding.GetBytes(SampleText.LoremIpsum));

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

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
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: a released modifier or lock key neither adds a key nor ends
	/// the hotkey being typed.
	/// </summary>
	[TestCase(KeyCode.VcLeftControl, EventMask.None)]
	[TestCase(KeyCode.VcLeftShift, EventMask.LeftCtrl)]
	[TestCase(KeyCode.VcCapsLock, EventMask.LeftCtrl)]
	public async Task HandleKeyReleasedAsync_Keeps_The_Input_On_A_Modifier_Key(KeyCode code, EventMask mask)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [ItemDtoFactory.CreateFileDto()];

		KeyStroke[] typed = [.. KeyStrokeFactory.CreateKeyStrokes(3)];

		sut
			.InputStack
			.AddRange(typed);

		// Act
		await sut.HandleKeyReleasedAsync(mask, code);

		// Assert
		sut.InputStack
			.Should()
			.Equal(typed);
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: a long pause between two keys starts a new hotkey.
	/// </summary>
	[TestCase(2.5, 2)]
	[TestCase(3.5, 1)]
	public async Task HandleKeyReleasedAsync_Keeps_The_Input_Only_Within_The_Pause(double pause, int keyCount)
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		KeyStroke[] hotkey =
		[
			new()
			{
				Code = KeyCode.VcZ,
				Mask = EventMask.LeftCtrl
			}
		];

		dto
			.Hotkeys
			.AddRange(hotkey.ToHotkeyDtos());

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<TimeProvider>(time));

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		await sut.HandleKeyReleasedAsync(EventMask.LeftCtrl, KeyCode.VcQ);

		time.Advance(TimeSpan.FromSeconds(pause));

		// Act
		await sut.HandleKeyReleasedAsync(EventMask.LeftCtrl, KeyCode.VcA);

		// Assert
		sut.InputStack
			.Should()
			.HaveCount(keyCount);
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: a matching hotkey copies the decrypted contents to the clipboard and shows a toast.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Sets_Text_To_Clipboard()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

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
					Contents = TextDefaults.Encoding.GetBytes(SampleText.LoremIpsum),
					IsValid = true
				});

			IContentCipher contentCipher = Substitute.For<IContentCipher>();

			contentCipher
				.TryDecryptContentsAsync(Arg.Any<FileDto>(), Arg.Any<byte[]>(), Arg.Any<string>())
				.Returns(TextDefaults.Encoding.GetBytes(SampleText.LoremIpsum));

			builder.RegisterInstance(contentCipher);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		sut
			.InputStack
			.AddRange(keyStrokes);

		// Act
		await sut.HandleKeyReleasedAsync(mask, code);

		// Assert
		await clipboard
			.Received(1)
			.SetTextAsync(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: a file removed from the tracked hierarchy no longer fires its hotkey.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Skips_A_File_Removed_During_Tracking()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		KeyStroke[] hotkey =
		[
			new()
			{
				Code = KeyCode.VcA,
				Mask = EventMask.LeftCtrl
			}
		];

		dto
			.Hotkeys
			.AddRange(hotkey.ToHotkeyDtos());

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [dto];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = hierarchy;

		hierarchy.Remove(dto);

		// Act
		await sut.HandleKeyReleasedAsync(EventMask.LeftCtrl, KeyCode.VcA);

		// Assert
		await dbAccess
			.DidNotReceive()
			.GetFileContentsAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.Receive" />: a released key message is handed to the asynchronous handler.
	/// </summary>
	[Test]
	public void Receive_Hands_Message_To_Handler()
	{
		// Arrange
		ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(exceptionHandler));

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		// Act
		sut.Receive(new GlobalKeyReleasedMessage(EventMask.LeftCtrl, KeyCode.VcA));

		// Assert
		exceptionHandler
			.Received(1)
			.Watch(Arg.Any<Task>());
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.StartTrackingAsync" />: the hook is started and the given hierarchy is tracked.
	/// </summary>
	[Test]
	public async Task StartTrackingAsync_Starts_Hook()
	{
		// Arrange
		TestGlobalHook hook = new();

		ExplorerItemDtoBase[] hierarchy = [ItemDtoFactory.CreateFileDto()];

		using AutoMock mock = AutoMock.GetLoose();

		GlobalHookRunner runner = mock.Create<GlobalHookRunner>(TypedParameter.From<IGlobalHook>(hook));

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>(TypedParameter.From<IGlobalHookRunner>(runner));

		// Act
		await sut.StartTrackingAsync(hierarchy);

		// Assert
		sut.IsRunning
			.Should()
			.BeTrue();

		sut.Hierarchy
			.Should()
			.BeSameAs(hierarchy);
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.StopTrackingAsync" />: the running hook is stopped and the tracked hierarchy and input stack are cleared.
	/// </summary>
	[Test]
	public async Task StopTrackingAsync_Stops_Hook()
	{
		// Arrange
		TestGlobalHook hook = new();

		using AutoMock mock = AutoMock.GetLoose();

		GlobalHookRunner runner = mock.Create<GlobalHookRunner>(TypedParameter.From<IGlobalHook>(hook));

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>(TypedParameter.From<IGlobalHookRunner>(runner));

		sut.Hierarchy = ItemDtoFactory.CreateFileDtos(5);

		sut
			.InputStack
			.AddRange(KeyStrokeFactory.CreateKeyStrokes(5));

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

		sut.Hierarchy
			.Should()
			.BeNull();

		sut.InputStack
			.Should()
			.BeEmpty();
	}
	#endregion
}
