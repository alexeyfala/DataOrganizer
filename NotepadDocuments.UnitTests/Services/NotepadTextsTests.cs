using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using EditorDocuments.Helpers;
using EditorDocuments.Interfaces;
using NotepadDocuments.Interfaces;
using NotepadDocuments.Services;
using Notifications.Constants;
using Notifications.Interfaces;
using NSubstitute;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace NotepadDocuments.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(NotepadTexts)}"" type")]
internal class NotepadTextsTests
{
	#region Methods
	/// <summary>
	/// <see cref="NotepadTexts.Erase" />: the text of a closed tab is erased from the disk.
	/// </summary>
	[Test]
	public void Erase_Erases_The_Text_Of_The_Tab()
	{
		// Arrange
		INotepadStore store = Substitute.For<INotepadStore>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			tab
				.Number
				.Returns(2);

			builder.RegisterInstance(store);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		// Act
		sut.Erase(tab);

		// Assert
		store
			.Received(1)
			.Erase(2);
	}

	/// <summary>
	/// <see cref="NotepadTexts.Erase" />: the file of a text that cannot be read stays on the disk.
	/// </summary>
	[Test]
	public void Erase_Keeps_The_File_Of_A_Text_That_Cannot_Be_Read()
	{
		// Arrange
		INotepadStore store = Substitute.For<INotepadStore>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			tab
				.IsReadOnly
				.Returns(true);

			tab
				.Number
				.Returns(2);

			builder.RegisterInstance(store);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		// Act
		sut.Erase(tab);

		// Assert
		store
			.DidNotReceive()
			.Erase(Arg.Any<int>());
	}

	/// <summary>
	/// <see cref="NotepadTexts.Follow" />: a new tab, which has nothing on the disk to read, shows its encoding.
	/// </summary>
	[Test]
	public void Follow_Refreshes_The_Encoding_Of_The_Tab()
	{
		// Arrange
		IEncodedDocument editor = Substitute.For<IEncodedDocument>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDocumentText document = Substitute.For<IDocumentText>();

			// An unstubbed observable gives a value at once, which would be a change of the text.
			document
				.Changed
				.Returns(Observable.Never<Unit>());

			editor
				.Document
				.Returns(document);

			tab
				.Editor
				.Returns(editor);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		// Act
		sut.Follow(tab);

		// Assert
		editor
			.Received()
			.RefreshEncoding();
	}

	/// <summary>
	/// <see cref="NotepadTexts.Follow" />: a write that keeps failing is reported once.
	/// </summary>
	[Test]
	public void Follow_Reports_A_Failed_Write_Once()
	{
		// Arrange
		Subject<Unit> changes = new();

		Action? write = null;

		INotificationService notification = Substitute.For<INotificationService>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			DocumentCodec codec = new();

			// A new tab is in UTF-8.
			codec.Read([], Encoding.UTF8.WebName);

			IDocumentText document = Substitute.For<IDocumentText>();

			IEncodedDocument editor = Substitute.For<IEncodedDocument>();

			INotepadStore store = Substitute.For<INotepadStore>();

			ITypingPauseFactory typingPauseFactory = Substitute.For<ITypingPauseFactory>();

			document
				.Changed
				.Returns(changes);

			document
				.Text
				.Returns("Text");

			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Returns(document);

			store
				.Write(1, Arg.Any<byte[]>())
				.Returns(false);

			tab
				.Editor
				.Returns(editor);

			tab
				.Number
				.Returns(1);

			typingPauseFactory
				.Create(Arg.Do<Action>(x => write = x))
				.Returns(Substitute.For<ITypingPause>());

			builder.RegisterInstance(notification);

			builder.RegisterInstance(store);

			builder.RegisterInstance(typingPauseFactory);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		sut.Follow(tab);

		changes.OnNext(Unit.Default);

		write!();

		// Act
		write!();

		// Assert
		notification
			.Received(1)
			.ShowErrorSnackbar(Arg.Any<string>(), SnackbarHostIdentifiers.Notepad);
	}

	/// <summary>
	/// <see cref="NotepadTexts.Follow" />: a write that fails again after one that succeeded is reported again.
	/// </summary>
	[Test]
	public void Follow_Reports_A_Failure_After_A_Write_That_Succeeded()
	{
		// Arrange
		Subject<Unit> changes = new();

		Action? write = null;

		INotificationService notification = Substitute.For<INotificationService>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			DocumentCodec codec = new();

			// A new tab is in UTF-8.
			codec.Read([], Encoding.UTF8.WebName);

			IDocumentText document = Substitute.For<IDocumentText>();

			IEncodedDocument editor = Substitute.For<IEncodedDocument>();

			INotepadStore store = Substitute.For<INotepadStore>();

			ITypingPauseFactory typingPauseFactory = Substitute.For<ITypingPauseFactory>();

			document
				.Changed
				.Returns(changes);

			document
				.Text
				.Returns("Text");

			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Returns(document);

			store
				.Write(1, Arg.Any<byte[]>())
				.Returns(false, true, false);

			tab
				.Editor
				.Returns(editor);

			tab
				.Number
				.Returns(1);

			typingPauseFactory
				.Create(Arg.Do<Action>(x => write = x))
				.Returns(Substitute.For<ITypingPause>());

			builder.RegisterInstance(notification);

			builder.RegisterInstance(store);

			builder.RegisterInstance(typingPauseFactory);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		sut.Follow(tab);

		changes.OnNext(Unit.Default);

		write!();

		write!();

		// Act
		write!();

		// Assert
		notification
			.Received(2)
			.ShowErrorSnackbar(Arg.Any<string>(), SnackbarHostIdentifiers.Notepad);
	}

	/// <summary>
	/// <see cref="NotepadTexts.Follow" />: a write that succeeds is not reported.
	/// </summary>
	[Test]
	public void Follow_Reports_Nothing_For_A_Write_That_Succeeded()
	{
		// Arrange
		Subject<Unit> changes = new();

		Action? write = null;

		INotificationService notification = Substitute.For<INotificationService>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			DocumentCodec codec = new();

			// A new tab is in UTF-8.
			codec.Read([], Encoding.UTF8.WebName);

			IDocumentText document = Substitute.For<IDocumentText>();

			IEncodedDocument editor = Substitute.For<IEncodedDocument>();

			INotepadStore store = Substitute.For<INotepadStore>();

			ITypingPauseFactory typingPauseFactory = Substitute.For<ITypingPauseFactory>();

			document
				.Changed
				.Returns(changes);

			document
				.Text
				.Returns("Text");

			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Returns(document);

			store
				.Write(1, Arg.Any<byte[]>())
				.Returns(true);

			tab
				.Editor
				.Returns(editor);

			tab
				.Number
				.Returns(1);

			typingPauseFactory
				.Create(Arg.Do<Action>(x => write = x))
				.Returns(Substitute.For<ITypingPause>());

			builder.RegisterInstance(notification);

			builder.RegisterInstance(store);

			builder.RegisterInstance(typingPauseFactory);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		sut.Follow(tab);

		changes.OnNext(Unit.Default);

		// Act
		write!();

		// Assert
		notification
			.DidNotReceive()
			.ShowErrorSnackbar(Arg.Any<string>(), Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="NotepadTexts.Follow" />: the text of a new tab is written in its encoding once typing pauses.
	/// </summary>
	[Test]
	public void Follow_Writes_The_Text_When_Typing_Pauses()
	{
		// Arrange
		Subject<Unit> changes = new();

		Action? write = null;

		INotepadStore store = Substitute.For<INotepadStore>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			DocumentCodec codec = new();

			// A new tab is in UTF-8.
			codec.Read([], Encoding.UTF8.WebName);

			IDocumentText document = Substitute.For<IDocumentText>();

			IEncodedDocument editor = Substitute.For<IEncodedDocument>();

			ITypingPauseFactory typingPauseFactory = Substitute.For<ITypingPauseFactory>();

			document
				.Changed
				.Returns(changes);

			document
				.Text
				.Returns("Text");

			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Returns(document);

			tab
				.Editor
				.Returns(editor);

			tab
				.Number
				.Returns(1);

			typingPauseFactory
				.Create(Arg.Do<Action>(x => write = x))
				.Returns(Substitute.For<ITypingPause>());

			builder.RegisterInstance(store);

			builder.RegisterInstance(typingPauseFactory);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		sut.Follow(tab);

		changes.OnNext(Unit.Default);

		// Act
		write!();

		// Assert
		store.Received(1).Write(
			1,
			Arg.Is<byte[]>(x => x.SequenceEqual(Encoding.UTF8.GetBytes("Text"))));
	}

	/// <summary>
	/// <see cref="NotepadTexts.Forget" />: a write that waits for a pause in typing is dropped with the closed tab.
	/// </summary>
	[Test]
	public void Forget_Drops_The_Pending_Write()
	{
		// Arrange
		Subject<Unit> changes = new();

		ITypingPause typingPause = Substitute.For<ITypingPause>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDocumentText document = Substitute.For<IDocumentText>();

			IEncodedDocument editor = Substitute.For<IEncodedDocument>();

			ITypingPauseFactory typingPauseFactory = Substitute.For<ITypingPauseFactory>();

			document
				.Changed
				.Returns(changes);

			editor
				.Document
				.Returns(document);

			tab
				.Editor
				.Returns(editor);

			typingPauseFactory
				.Create(Arg.Any<Action>())
				.Returns(typingPause);

			builder.RegisterInstance(typingPauseFactory);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		sut.Follow(tab);

		changes.OnNext(Unit.Default);

		// Act
		sut.Forget(tab);

		// Assert
		typingPause
			.Received(1)
			.Dispose();
	}

	/// <summary>
	/// <see cref="NotepadTexts.Forget" />: the failed write of a tab is forgotten with it, so a tab followed again reports
	/// its next failure.
	/// </summary>
	[Test]
	public void Forget_Forgets_The_Failed_Write()
	{
		// Arrange
		Subject<Unit> changes = new();

		Action? write = null;

		INotificationService notification = Substitute.For<INotificationService>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			DocumentCodec codec = new();

			// A new tab is in UTF-8.
			codec.Read([], Encoding.UTF8.WebName);

			IDocumentText document = Substitute.For<IDocumentText>();

			IEncodedDocument editor = Substitute.For<IEncodedDocument>();

			INotepadStore store = Substitute.For<INotepadStore>();

			ITypingPauseFactory typingPauseFactory = Substitute.For<ITypingPauseFactory>();

			document
				.Changed
				.Returns(changes);

			document
				.Text
				.Returns("Text");

			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Returns(document);

			store
				.Write(1, Arg.Any<byte[]>())
				.Returns(false);

			tab
				.Editor
				.Returns(editor);

			tab
				.Number
				.Returns(1);

			typingPauseFactory
				.Create(Arg.Do<Action>(x => write = x))
				.Returns(Substitute.For<ITypingPause>());

			builder.RegisterInstance(notification);

			builder.RegisterInstance(store);

			builder.RegisterInstance(typingPauseFactory);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		sut.Follow(tab);

		changes.OnNext(Unit.Default);

		write!();

		sut.Forget(tab);

		sut.Follow(tab);

		changes.OnNext(Unit.Default);

		// Act
		write!();

		// Assert
		notification
			.Received(2)
			.ShowErrorSnackbar(Arg.Any<string>(), SnackbarHostIdentifiers.Notepad);
	}

	/// <summary>
	/// <see cref="NotepadTexts.Forget" />: a change of the text of a closed tab puts no write off.
	/// </summary>
	[Test]
	public void Forget_Stops_Writing_The_Text()
	{
		// Arrange
		Subject<Unit> changes = new();

		ITypingPauseFactory typingPauseFactory = Substitute.For<ITypingPauseFactory>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDocumentText document = Substitute.For<IDocumentText>();

			IEncodedDocument editor = Substitute.For<IEncodedDocument>();

			document
				.Changed
				.Returns(changes);

			editor
				.Document
				.Returns(document);

			tab
				.Editor
				.Returns(editor);

			builder.RegisterInstance(typingPauseFactory);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		sut.Follow(tab);

		sut.Forget(tab);

		// Act
		changes.OnNext(Unit.Default);

		// Assert
		typingPauseFactory
			.DidNotReceive()
			.Create(Arg.Any<Action>());
	}

	/// <summary>
	/// <see cref="NotepadTexts.Load" />: a text that cannot be read is not written back, so a change puts no write off.
	/// </summary>
	[Test]
	public void Load_Does_Not_Follow_A_Text_That_Cannot_Be_Read()
	{
		// Arrange
		Subject<Unit> changes = new();

		ITypingPauseFactory typingPauseFactory = Substitute.For<ITypingPauseFactory>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDocumentText document = Substitute.For<IDocumentText>();

			IEncodedDocument editor = Substitute.For<IEncodedDocument>();

			INotepadStore store = Substitute.For<INotepadStore>();

			document
				.Changed
				.Returns(changes);

			editor
				.Document
				.Returns(document);

			// The file cannot be read: an unstubbed call gives an empty array.
			store
				.Read(1)
				.Returns((byte[]?)null);

			tab
				.Editor
				.Returns(editor);

			tab
				.Number
				.Returns(1);

			builder.RegisterInstance(store);

			builder.RegisterInstance(typingPauseFactory);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		sut.Load(tab);

		// Act
		changes.OnNext(Unit.Default);

		// Assert
		typingPauseFactory
			.DidNotReceive()
			.Create(Arg.Any<Action>());
	}

	/// <summary>
	/// <see cref="NotepadTexts.Load" />: a text that its encoding could not hold is in UTF-8 from then on.
	/// </summary>
	[Test]
	public void Load_Keeps_A_Text_In_Utf8_Once_Its_Encoding_Could_Not_Hold_It()
	{
		// Arrange
		DocumentCodec codec = new();

		Subject<Unit> changes = new();

		Action? write = null;

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] contents = [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("Text")];

			IDocumentText document = Substitute.For<IDocumentText>();

			IEncodedDocument editor = Substitute.For<IEncodedDocument>();

			INotepadStore store = Substitute.For<INotepadStore>();

			ITypingPauseFactory typingPauseFactory = Substitute.For<ITypingPauseFactory>();

			document
				.Changed
				.Returns(changes);

			// A lone surrogate, which UTF-16 cannot hold either.
			document
				.Text
				.Returns("Text\uD800");

			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Returns(document);

			store
				.Read(1)
				.Returns(contents);

			tab
				.Editor
				.Returns(editor);

			tab
				.Number
				.Returns(1);

			typingPauseFactory
				.Create(Arg.Do<Action>(x => write = x))
				.Returns(Substitute.For<ITypingPause>());

			builder.RegisterInstance(store);

			builder.RegisterInstance(typingPauseFactory);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		sut.Load(tab);

		changes.OnNext(Unit.Default);

		// Act
		write!();

		// Assert
		codec.Encoding
			.Should()
			.Be(Encoding.UTF8.WebName);
	}

	/// <summary>
	/// <see cref="NotepadTexts.Load" />: a text in UTF-8 that holds a lone surrogate is written in UTF-8 as it is, with no
	/// switch of its encoding.
	/// </summary>
	[Test]
	public void Load_Keeps_The_Encoding_Of_A_Utf8_Text_With_A_Lone_Surrogate()
	{
		// Arrange
		Subject<Unit> changes = new();

		Action? write = null;

		IEncodedDocument editor = Substitute.For<IEncodedDocument>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			DocumentCodec codec = new();

			IDocumentText document = Substitute.For<IDocumentText>();

			INotepadStore store = Substitute.For<INotepadStore>();

			ITypingPauseFactory typingPauseFactory = Substitute.For<ITypingPauseFactory>();

			document
				.Changed
				.Returns(changes);

			// UTF-8 cannot hold a lone surrogate either.
			document
				.Text
				.Returns("Text\uD800");

			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Returns(document);

			store
				.Read(1)
				.Returns(Encoding.UTF8.GetBytes("Text"));

			tab
				.Editor
				.Returns(editor);

			tab
				.Number
				.Returns(1);

			typingPauseFactory
				.Create(Arg.Do<Action>(x => write = x))
				.Returns(Substitute.For<ITypingPause>());

			builder.RegisterInstance(store);

			builder.RegisterInstance(typingPauseFactory);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		sut.Load(tab);

		changes.OnNext(Unit.Default);

		// Act
		write!();

		// Assert
		editor
			.DidNotReceive()
			.RefreshEncoding();
	}

	/// <summary>
	/// <see cref="NotepadTexts.Load" />: bytes that are not text leave the tab read-only.
	/// </summary>
	[Test]
	public void Load_Makes_The_Tab_Read_Only_When_Its_Bytes_Are_Not_Text()
	{
		// Arrange
		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			DocumentCodec codec = new();

			IEncodedDocument editor = Substitute.For<IEncodedDocument>();

			INotepadStore store = Substitute.For<INotepadStore>();

			editor
				.Codec
				.Returns(codec);

			// A zero byte with an odd count of bytes, so neither UTF-16 nor any code page reads them.
			store
				.Read(1)
				.Returns([0x00, 0xFF, 0x00]);

			tab
				.Editor
				.Returns(editor);

			tab
				.Number
				.Returns(1);

			builder.RegisterInstance(store);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		// Act
		bool result = sut.Load(tab);

		// Assert
		result
			.Should()
			.BeFalse();

		tab.IsReadOnly
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadTexts.Load" />: a file that cannot be read leaves the tab read-only.
	/// </summary>
	[Test]
	public void Load_Makes_The_Tab_Read_Only_When_Its_File_Cannot_Be_Read()
	{
		// Arrange
		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INotepadStore store = Substitute.For<INotepadStore>();

			// An unstubbed call gives an empty array.
			store
				.Read(1)
				.Returns((byte[]?)null);

			tab
				.Number
				.Returns(1);

			builder.RegisterInstance(store);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		// Act
		bool result = sut.Load(tab);

		// Assert
		result
			.Should()
			.BeFalse();

		tab.IsReadOnly
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadTexts.Load" />: a change of the text puts its write off until typing pauses.
	/// </summary>
	[Test]
	public void Load_Puts_The_Write_Off_Until_Typing_Pauses()
	{
		// Arrange
		Subject<Unit> changes = new();

		INotepadStore store = Substitute.For<INotepadStore>();

		ITypingPause typingPause = Substitute.For<ITypingPause>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			DocumentCodec codec = new();

			IDocumentText document = Substitute.For<IDocumentText>();

			IEncodedDocument editor = Substitute.For<IEncodedDocument>();

			ITypingPauseFactory typingPauseFactory = Substitute.For<ITypingPauseFactory>();

			document
				.Changed
				.Returns(changes);

			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Returns(document);

			store
				.Read(1)
				.Returns(Encoding.UTF8.GetBytes("Text"));

			tab
				.Editor
				.Returns(editor);

			tab
				.Number
				.Returns(1);

			typingPauseFactory
				.Create(Arg.Any<Action>())
				.Returns(typingPause);

			builder.RegisterInstance(store);

			builder.RegisterInstance(typingPauseFactory);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		sut.Load(tab);

		// Act
		changes.OnNext(Unit.Default);

		// Assert
		typingPause
			.Received(1)
			.Restart();

		store
			.DidNotReceive()
			.Write(Arg.Any<int>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="NotepadTexts.Load" />: the text is read in the encoding of the tab.
	/// </summary>
	[Test]
	public void Load_Reads_The_Text_In_The_Encoding_Of_The_Tab()
	{
		// Arrange
		IEncodedDocument editor = Substitute.For<IEncodedDocument>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			Encoding koi8 = CodePagesEncodingProvider.Instance.GetEncoding("koi8-r")!;

			DocumentCodec codec = new();

			// The settings keep KOI8-R for the tab, which no system takes for the bytes of a file.
			codec.Read([], koi8.WebName);

			IDocumentText document = Substitute.For<IDocumentText>();

			INotepadStore store = Substitute.For<INotepadStore>();

			// An unstubbed observable gives a value at once, which would be a change of the text.
			document
				.Changed
				.Returns(Observable.Never<Unit>());

			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Returns(document);

			store
				.Read(1)
				.Returns(koi8.GetBytes("Привет"));

			tab
				.Editor
				.Returns(editor);

			tab
				.Number
				.Returns(1);

			builder.RegisterInstance(store);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		// Act
		bool result = sut.Load(tab);

		// Assert
		result
			.Should()
			.BeTrue();

		editor
			.Received(1)
			.ReplaceText("Привет");
	}

	/// <summary>
	/// <see cref="NotepadTexts.Load" />: a text that its encoding cannot hold switches to UTF-8 before it is written, so
	/// the settings name UTF-8 first.
	/// </summary>
	[Test]
	public void Load_Refreshes_The_Encoding_Before_Writing_A_Text_In_Utf8()
	{
		// Arrange
		Subject<Unit> changes = new();

		Action? write = null;

		IEncodedDocument editor = Substitute.For<IEncodedDocument>();

		INotepadStore store = Substitute.For<INotepadStore>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] contents = [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("Text")];

			DocumentCodec codec = new();

			IDocumentText document = Substitute.For<IDocumentText>();

			ITypingPauseFactory typingPauseFactory = Substitute.For<ITypingPauseFactory>();

			document
				.Changed
				.Returns(changes);

			// A lone surrogate, which UTF-16 cannot hold either.
			document
				.Text
				.Returns("Text\uD800");

			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Returns(document);

			store
				.Read(1)
				.Returns(contents);

			tab
				.Editor
				.Returns(editor);

			tab
				.Number
				.Returns(1);

			typingPauseFactory
				.Create(Arg.Do<Action>(x => write = x))
				.Returns(Substitute.For<ITypingPause>());

			builder.RegisterInstance(store);

			builder.RegisterInstance(typingPauseFactory);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		sut.Load(tab);

		changes.OnNext(Unit.Default);

		// Act
		write!();

		// Assert
		Received.InOrder(() =>
		{
			editor.RefreshEncoding();

			store.Write(1, Arg.Any<byte[]>());
		});
	}

	/// <summary>
	/// <see cref="NotepadTexts.Load" />: a text that its encoding cannot hold is written in UTF-8, where a lone surrogate
	/// becomes the replacement character.
	/// </summary>
	[Test]
	public void Load_Writes_A_Text_That_Its_Encoding_Cannot_Hold_In_Utf8()
	{
		// Arrange
		Subject<Unit> changes = new();

		Action? write = null;

		INotepadStore store = Substitute.For<INotepadStore>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] contents = [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("Text")];

			DocumentCodec codec = new();

			IDocumentText document = Substitute.For<IDocumentText>();

			IEncodedDocument editor = Substitute.For<IEncodedDocument>();

			ITypingPauseFactory typingPauseFactory = Substitute.For<ITypingPauseFactory>();

			document
				.Changed
				.Returns(changes);

			// A lone surrogate, which UTF-16 cannot hold either.
			document
				.Text
				.Returns("Text\uD800");

			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Returns(document);

			store
				.Read(1)
				.Returns(contents);

			tab
				.Editor
				.Returns(editor);

			tab
				.Number
				.Returns(1);

			typingPauseFactory
				.Create(Arg.Do<Action>(x => write = x))
				.Returns(Substitute.For<ITypingPause>());

			builder.RegisterInstance(store);

			builder.RegisterInstance(typingPauseFactory);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		sut.Load(tab);

		changes.OnNext(Unit.Default);

		// Act
		write!();

		// Assert
		store.Received(1).Write(
			1,
			Arg.Is<byte[]>(x => x.SequenceEqual(Encoding.UTF8.GetBytes("Text�"))));
	}

	/// <summary>
	/// <see cref="NotepadTexts.Load" />: a changed text is written in its encoding once typing pauses.
	/// </summary>
	[Test]
	public void Load_Writes_The_Changed_Text_When_Typing_Pauses()
	{
		// Arrange
		byte[] expected = [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("Changed")];

		Subject<Unit> changes = new();

		Action? write = null;

		INotepadStore store = Substitute.For<INotepadStore>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] contents = [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("Text")];

			DocumentCodec codec = new();

			IDocumentText document = Substitute.For<IDocumentText>();

			IEncodedDocument editor = Substitute.For<IEncodedDocument>();

			ITypingPauseFactory typingPauseFactory = Substitute.For<ITypingPauseFactory>();

			document
				.Changed
				.Returns(changes);

			document
				.Text
				.Returns("Changed");

			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Returns(document);

			store
				.Read(1)
				.Returns(contents);

			tab
				.Editor
				.Returns(editor);

			tab
				.Number
				.Returns(1);

			typingPauseFactory
				.Create(Arg.Do<Action>(x => write = x))
				.Returns(Substitute.For<ITypingPause>());

			builder.RegisterInstance(store);

			builder.RegisterInstance(typingPauseFactory);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		sut.Load(tab);

		changes.OnNext(Unit.Default);

		// Act
		write!();

		// Assert
		store.Received(1).Write(
			1,
			Arg.Is<byte[]>(x => x.SequenceEqual(expected)));
	}

	/// <summary>
	/// <see cref="NotepadTexts.WritePending" />: the texts that still wait for a pause in typing are written at once.
	/// </summary>
	[Test]
	public void WritePending_Writes_The_Texts_Waiting_For_A_Pause()
	{
		// Arrange
		Subject<Unit> changes = new();

		ITypingPause typingPause = Substitute.For<ITypingPause>();

		INotepadTab tab = Substitute.For<INotepadTab>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDocumentText document = Substitute.For<IDocumentText>();

			IEncodedDocument editor = Substitute.For<IEncodedDocument>();

			ITypingPauseFactory typingPauseFactory = Substitute.For<ITypingPauseFactory>();

			document
				.Changed
				.Returns(changes);

			editor
				.Document
				.Returns(document);

			tab
				.Editor
				.Returns(editor);

			typingPauseFactory
				.Create(Arg.Any<Action>())
				.Returns(typingPause);

			builder.RegisterInstance(typingPauseFactory);
		});

		NotepadTexts sut = mock.Create<NotepadTexts>();

		sut.Follow(tab);

		changes.OnNext(Unit.Default);

		// Act
		sut.WritePending();

		// Assert
		typingPause
			.Received(1)
			.Flush();
	}
	#endregion
}
