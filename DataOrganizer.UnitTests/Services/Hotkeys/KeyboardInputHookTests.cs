using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Extensions.Hotkeys;
using DataOrganizer.Helpers.Seeding;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Favorites;
using DataOrganizer.Messages.Hotkeys;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.Services.Hotkeys;
using DataOrganizer.Services.Runtime;
using Diagnostics.Interfaces;
using EditorDocuments.Dto;
using FileContents.Dto;
using FileContents.Enums;
using FileContents.Interfaces;
using FileContents.Services;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Hierarchy.UnitTests.Factories;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Repository.Dto;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Services;
using SharpHook;
using SharpHook.Data;
using SharpHook.Testing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Dto;
using TextProcessing.Constants;
using Threading.Interfaces;
using Threading.UnitTests.Fakes;

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
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: the hotkey of an encrypted file brings the main window
	/// back with its favorites, so that the password is asked for over it.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Brings_Back_The_Main_Window_With_The_Favorites_For_An_Encrypted_File()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

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

			builder
				.RegisterType<HotkeyContentCopier>()
				.As<IHotkeyContentCopier>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		sut
			.InputStack
			.AddRange(keyStrokes);

		// Act
		await sut.HandleKeyReleasedAsync(mask, code);

		// Assert
		steps
			.Should()
			.Equal("activate", "favorites");
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
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(Arg.Any<FileDto>(), Arg.Any<string>())
				.Returns(new FileContentsReading
				{
					Contents = TextDefaults.Encoding.GetBytes(SampleText.LoremIpsum),
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(fileContentsReader);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<HotkeyContentCopier>()
				.As<IHotkeyContentCopier>();
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
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: a hotkey of a file without text puts nothing in the clipboard.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Copies_Nothing_From_An_Empty_File()
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

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(dto, Arg.Any<string>())
				.Returns(new FileContentsReading
				{
					Contents = [],
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(fileContentsReader);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<ClipboardTextWriter>()
				.As<IClipboardTextWriter>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<HotkeyContentCopier>()
				.As<IHotkeyContentCopier>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		// Act
		await sut.HandleKeyReleasedAsync(EventMask.LeftCtrl, KeyCode.VcA);

		// Assert
		await clipboard
			.DidNotReceive()
			.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<bool>());
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: a hotkey of a file whose contents fail their check puts
	/// nothing in the clipboard.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Copies_Nothing_From_Damaged_Contents()
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

		IClipboardAccessor clipboard = Substitute.For<IClipboardAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(dto, Arg.Any<string>())
				.Returns(new FileContentsReading
				{
					Status = FileContentsStatus.Damaged
				});

			builder.RegisterInstance(fileContentsReader);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<ClipboardTextWriter>()
				.As<IClipboardTextWriter>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<HotkeyContentCopier>()
				.As<IHotkeyContentCopier>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		// Act
		await sut.HandleKeyReleasedAsync(EventMask.LeftCtrl, KeyCode.VcA);

		// Assert
		await clipboard
			.DidNotReceive()
			.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<bool>());
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
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			// The start of a PNG image
			fileContentsReader
				.ReadAsync(Arg.Any<FileDto>(), Arg.Any<string>())
				.Returns(new FileContentsReading
				{
					Contents = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D],
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(fileContentsReader);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<ClipboardTextWriter>()
				.As<IClipboardTextWriter>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<HotkeyContentCopier>()
				.As<IHotkeyContentCopier>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		// Act
		await sut.HandleKeyReleasedAsync(mask, code);

		// Assert
		await clipboard
			.DidNotReceive()
			.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<bool>());
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
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(Arg.Any<FileDto>(), Arg.Any<string>())
				.Returns(new FileContentsReading
				{
					Contents = contents,
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(fileContentsReader);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<ClipboardTextWriter>()
				.As<IClipboardTextWriter>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<HotkeyContentCopier>()
				.As<IHotkeyContentCopier>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		// Act
		await sut.HandleKeyReleasedAsync(mask, code);

		// Assert
		await clipboard
			.Received(1)
			.SetTextAsync("Hi", html: null, rtf: null, isSensitive: false, isRestored: false);
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
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(Arg.Any<FileDto>(), Arg.Any<string>())
				.Returns(new FileContentsReading
				{
					Contents = CodePagesEncodingProvider.Instance.GetEncoding(866)!.GetBytes(text),
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(fileContentsReader);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<ClipboardTextWriter>()
				.As<IClipboardTextWriter>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder
				.RegisterType<HotkeyContentCopier>()
				.As<IHotkeyContentCopier>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		// Act
		await sut.HandleKeyReleasedAsync(mask, code);

		// Assert
		await clipboard
			.Received(1)
			.SetTextAsync(text, html: null, rtf: null, isSensitive: false, isRestored: false);
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
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(Arg.Any<FileDto>(), Arg.Any<string>())
				.Returns(new FileContentsReading
				{
					Contents = TextDefaults.Encoding.GetBytes(SampleText.LoremIpsum),
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(fileContentsReader);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<ClipboardTextWriter>()
				.As<IClipboardTextWriter>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<HotkeyContentCopier>()
				.As<IHotkeyContentCopier>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		// Act
		await sut.HandleKeyReleasedAsync(typedMask, KeyCode.VcA);

		// Assert
		await clipboard
			.Received(1)
			.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), isSensitive: false, isRestored: false);
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

		IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// Contents that fail their check end the copy right after the read.
			fileContentsReader
				.ReadAsync(dto, Arg.Any<string>())
				.Returns(new FileContentsReading
				{
					Status = FileContentsStatus.Damaged
				});

			builder.RegisterInstance(fileContentsReader);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<HotkeyContentCopier>()
				.As<IHotkeyContentCopier>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = hierarchy;

		hierarchy.Add(dto);

		// Act
		await sut.HandleKeyReleasedAsync(EventMask.LeftCtrl, KeyCode.VcA);

		// Assert
		await fileContentsReader
			.Received(1)
			.ReadAsync(dto, Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: protected contents are flagged sensitive.
	/// </summary>
	[Test]
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
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(Arg.Any<FileDto>(), Arg.Any<string>())
				.Returns(new FileContentsReading
				{
					Contents = TextDefaults.Encoding.GetBytes(SampleText.LoremIpsum),
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(fileContentsReader);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<ClipboardTextWriter>()
				.As<IClipboardTextWriter>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<HotkeyContentCopier>()
				.As<IHotkeyContentCopier>();
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
			.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), isSensitive: true, isRestored: false);

		await clipboard
			.DidNotReceive()
			.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), isSensitive: false, isRestored: false);
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: a file whose text is not copied stays out of the copy
	/// history.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Keeps_The_Copy_History_When_Nothing_Is_Copied(
		[Values(FileTextStatus.Empty, FileTextStatus.NotText, FileTextStatus.Damaged, FileTextStatus.Closed)] FileTextStatus status)
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

		ICopyHistoryRecorder copyHistory = Substitute.For<ICopyHistoryRecorder>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileTextCopier fileTextCopier = Substitute.For<IFileTextCopier>();

			fileTextCopier
				.CopyAsync(dto, Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(status);

			builder.RegisterInstance(copyHistory);

			builder.RegisterInstance(fileTextCopier);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<HotkeyContentCopier>()
				.As<IHotkeyContentCopier>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		// Act
		await sut.HandleKeyReleasedAsync(EventMask.LeftCtrl, KeyCode.VcA);

		// Assert
		copyHistory
			.DidNotReceive()
			.Add(Arg.Any<FileDto>(), Arg.Any<bool>());
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
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: the hotkey of a file that is not encrypted leaves the main
	/// window where it is.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Leaves_The_Main_Window_Alone_For_A_File_That_Is_Not_Encrypted()
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

		IMainWindowActivator mainWindowActivator = Substitute.For<IMainWindowActivator>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(mainWindowActivator);

			builder
				.RegisterType<HotkeyContentCopier>()
				.As<IHotkeyContentCopier>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		sut
			.InputStack
			.AddRange(keyStrokes);

		// Act
		await sut.HandleKeyReleasedAsync(mask, code);

		// Assert
		mainWindowActivator
			.DidNotReceive()
			.ActivateMainWindow();
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: a file copied by its hotkey goes into the copy history,
	/// and a shown list moves it at once.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Puts_The_File_Into_The_Copy_History()
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

		ICopyHistoryRecorder copyHistory = Substitute.For<ICopyHistoryRecorder>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileTextCopier fileTextCopier = Substitute.For<IFileTextCopier>();

			fileTextCopier
				.CopyAsync(dto, Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(FileTextStatus.Text);

			builder.RegisterInstance(copyHistory);

			builder.RegisterInstance(fileTextCopier);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<HotkeyContentCopier>()
				.As<IHotkeyContentCopier>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		// Act
		await sut.HandleKeyReleasedAsync(EventMask.LeftCtrl, KeyCode.VcA);

		// Assert
		copyHistory
			.Received(1)
			.Add(dto, true);
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
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(Arg.Any<FileDto>(), Arg.Any<string>())
				.Returns(new FileContentsReading
				{
					Contents = TextDefaults.Encoding.GetBytes(SampleText.LoremIpsum),
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(fileContentsReader);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder.RegisterInstance(clipboard);

			builder
				.RegisterType<ClipboardTextWriter>()
				.As<IClipboardTextWriter>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<HotkeyContentCopier>()
				.As<IHotkeyContentCopier>();
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
			.SetTextAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), isSensitive: false, isRestored: false);
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

		IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(fileContentsReader);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<HotkeyContentCopier>()
				.As<IHotkeyContentCopier>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = hierarchy;

		hierarchy.Remove(dto);

		// Act
		await sut.HandleKeyReleasedAsync(EventMask.LeftCtrl, KeyCode.VcA);

		// Assert
		await fileContentsReader
			.DidNotReceive()
			.ReadAsync(Arg.Any<FileDto>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />: the decrypted contents are wiped once their text is read.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Zeroes_The_Decrypted_Contents(
		[Values(EncryptionStatus.Encrypted, EncryptionStatus.Decrypted)] EncryptionStatus encryptionStatus)
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto(encryptionStatus: encryptionStatus);

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

		byte[] decrypted = TextDefaults.Encoding.GetBytes(SampleText.LoremIpsum);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileContentsReader fileContentsReader = Substitute.For<IFileContentsReader>();

			fileContentsReader
				.ReadAsync(dto, Arg.Any<string>())
				.Returns(new FileContentsReading
				{
					Contents = decrypted,
					Status = FileContentsStatus.Opened
				});

			builder.RegisterInstance(fileContentsReader);

			builder
				.RegisterType<FileTextReader>()
				.As<IFileTextReader>();

			builder
				.RegisterType<FileTextCopier>()
				.As<IFileTextCopier>();

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<HotkeyContentCopier>()
				.As<IHotkeyContentCopier>();
		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		sut.Hierarchy = [dto];

		// Act
		await sut.HandleKeyReleasedAsync(EventMask.LeftCtrl, KeyCode.VcA);

		// Assert
		decrypted
			.Should()
			.OnlyContain(static x => x == 0);
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
	/// <see cref="KeyboardInputHook.StartTrackingAsync" />: the hook is started and the tree of the work session is
	/// tracked.
	/// </summary>
	[Test]
	public async Task StartTrackingAsync_Starts_Hook()
	{
		// Arrange
		TestGlobalHook hook = new();

		WorkSession session = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterInstance(session)
			.AsImplementedInterfaces());

		GlobalHookRunner runner = mock.Create<GlobalHookRunner>(TypedParameter.From<IGlobalHook>(hook));

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>(TypedParameter.From<IGlobalHookRunner>(runner));

		// Act
		await sut.StartTrackingAsync();

		// Assert
		sut.IsRunning
			.Should()
			.BeTrue();

		sut.Hierarchy
			.Should()
			.BeSameAs(session.Hierarchy);
	}

	/// <summary>
	/// <see cref="KeyboardInputHook.StopTrackingAsync" />: a stop gives the lock back, so the next stop is not left waiting.
	/// </summary>
	[Test]
	public async Task StopTrackingAsync_Gives_The_Lock_Back()
	{
		// Arrange
		TestGlobalHook hook = new();

		using AutoMock mock = AutoMock.GetLoose();

		GlobalHookRunner runner = mock.Create<GlobalHookRunner>(TypedParameter.From<IGlobalHook>(hook));

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>(TypedParameter.From<IGlobalHookRunner>(runner));

		await sut.StopTrackingAsync();

		using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));

		// Act
		Func<Task> stopAgain = () => sut.StopTrackingAsync(timeout.Token);

		// Assert
		await stopAgain
			.Should()
			.NotThrowAsync();
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
