using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using EditorDocuments.Helpers;
using EditorDocuments.Interfaces;
using EditorDocuments.Services;
using Notifications.Constants;
using Notifications.Interfaces;
using NSubstitute;
using System.Text;
using System.Threading.Tasks;
using Threading.Interfaces;
using Threading.UnitTests.Fakes;

namespace EditorDocuments.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(DocumentEncodingSwitcher)}"" type")]
internal class DocumentEncodingSwitcherTests
{
	#region Methods
	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: a text that is not read yet has no encoding to change, so a
	/// choice changes nothing and tells nothing.
	/// </summary>
	[Test]
	public async Task SwitchAsync_Changes_Nothing_Before_The_Text_Is_Read()
	{
		// Arrange
		INotificationService notification = Substitute.For<INotificationService>();

		IEncodedDocument editor = Substitute.For<IEncodedDocument>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			DocumentCodec codec = new();

			editor
				.Codec
				.Returns(codec);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(notification);
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		// Act
		bool result = await sut.SwitchAsync(
			editor,
			Encoding.Unicode.WebName,
			SnackbarHostIdentifiers.Main,
			"file");

		// Assert
		result
			.Should()
			.BeFalse();

		notification
			.DidNotReceive()
			.ShowWarningSnackbar(Arg.Any<string>(), Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: a choice of the encoding the text is in already, or of none,
	/// changes nothing and tells nothing.
	/// </summary>
	[Test]
	[TestCase("utf-8")]
	[TestCase(null)]
	public async Task SwitchAsync_Changes_Nothing_Without_Another_Encoding(string? encoding)
	{
		// Arrange
		INotificationService notification = Substitute.For<INotificationService>();

		IEncodedDocument editor = Substitute.For<IEncodedDocument>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			DocumentCodec codec = new();

			codec.Read(Encoding.UTF8.GetBytes("Text"), Encoding.UTF8.WebName);

			editor
				.Codec
				.Returns(codec);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(notification);
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		// Act
		bool result = await sut.SwitchAsync(
			editor,
			encoding,
			SnackbarHostIdentifiers.Notepad,
			"tab 1");

		// Assert
		result
			.Should()
			.BeFalse();

		notification
			.DidNotReceive()
			.ShowWarningSnackbar(Arg.Any<string>(), Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: an encoding that cannot read the text gives way to the one the
	/// text is in: here UTF-16 for an odd number of bytes.
	/// </summary>
	[Test]
	public async Task SwitchAsync_Gives_Way_When_The_Choice_Cannot_Read_The_Text()
	{
		// Arrange
		DocumentCodec codec = new();

		codec.Read(Encoding.UTF8.GetBytes("Tex"), Encoding.UTF8.WebName);

		IEncodedDocument editor = Substitute.For<IEncodedDocument>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Text
				.Returns("Tex");

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		// Act
		bool result = await sut.SwitchAsync(
			editor,
			Encoding.Unicode.WebName,
			SnackbarHostIdentifiers.Notepad,
			"tab 1");

		// Assert
		result
			.Should()
			.BeFalse();

		codec.Encoding
			.Should()
			.Be(Encoding.UTF8.WebName);

		editor
			.Received()
			.RefreshEncoding();
	}

	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: a text that its encoding cannot hold has no bytes to read again,
	/// so the encoding stays.
	/// </summary>
	[Test]
	public async Task SwitchAsync_Gives_Way_When_The_Text_Has_No_Bytes_In_Its_Encoding()
	{
		// Arrange
		DocumentCodec codec = new();

		codec.Read(Encoding.UTF8.GetBytes("Text"), Encoding.UTF8.WebName);

		IEncodedDocument editor = Substitute.For<IEncodedDocument>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			editor
				.Codec
				.Returns(codec);

			// A lone surrogate, which no encoding holds.
			editor
				.Document
				.Text
				.Returns("Text\uD800");

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		// Act
		bool result = await sut.SwitchAsync(
			editor,
			Encoding.Unicode.WebName,
			SnackbarHostIdentifiers.Notepad,
			"tab 1");

		// Assert
		result
			.Should()
			.BeFalse();

		codec.Encoding
			.Should()
			.Be(Encoding.UTF8.WebName);

		editor
			.Received()
			.RefreshEncoding();
	}

	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: an encoding that cannot read the text leaves the text as it
	/// was.
	/// </summary>
	[Test]
	public async Task SwitchAsync_Keeps_The_Text_When_The_Choice_Cannot_Read_It()
	{
		// Arrange
		IEncodedDocument editor = Substitute.For<IEncodedDocument>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			DocumentCodec codec = new();

			codec.Read(Encoding.UTF8.GetBytes("Tex"), Encoding.UTF8.WebName);

			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Text
				.Returns("Tex");

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		// Act
		await sut.SwitchAsync(
			editor,
			Encoding.Unicode.WebName,
			SnackbarHostIdentifiers.Main,
			"file");

		// Assert
		editor
			.DidNotReceive()
			.ReplaceText(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: the codec takes the chosen encoding when it reads the text
	/// again in it.
	/// </summary>
	[Test]
	public async Task SwitchAsync_Leaves_The_Codec_In_The_Chosen_Encoding()
	{
		// Arrange
		DocumentCodec codec = new();

		codec.Read(Encoding.UTF8.GetBytes("Text"), Encoding.UTF8.WebName);

		IEncodedDocument editor = Substitute.For<IEncodedDocument>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Text
				.Returns("Text");

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		// Act
		await sut.SwitchAsync(
			editor,
			Encoding.Unicode.WebName,
			SnackbarHostIdentifiers.Notepad,
			"tab 1");

		// Assert
		codec.Encoding
			.Should()
			.Be(Encoding.Unicode.WebName);
	}

	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: the text stays as it is until the posted work runs, after the
	/// binding that wrote the choice is done.
	/// </summary>
	[Test]
	public void SwitchAsync_Leaves_The_Text_Until_The_Posted_Work_Runs()
	{
		// Arrange
		IEncodedDocument editor = Substitute.For<IEncodedDocument>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			DocumentCodec codec = new();

			codec.Read(Encoding.UTF8.GetBytes("Text"), Encoding.UTF8.WebName);

			// A switch run at once would read this text again and replace it.
			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Text
				.Returns("Text");
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		// Act
		_ = sut.SwitchAsync(
			editor,
			Encoding.Unicode.WebName,
			SnackbarHostIdentifiers.Main,
			"file");

		// Assert
		editor
			.DidNotReceive()
			.ReplaceText(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: another encoding reads the text again from the same bytes and
	/// answers so.
	/// </summary>
	[Test]
	public async Task SwitchAsync_Reads_The_Text_Again_In_The_Chosen_Encoding()
	{
		// Arrange
		IEncodedDocument editor = Substitute.For<IEncodedDocument>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			DocumentCodec codec = new();

			codec.Read(Encoding.UTF8.GetBytes("Text"), Encoding.UTF8.WebName);

			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Text
				.Returns("Text");

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		// Act
		bool result = await sut.SwitchAsync(
			editor,
			Encoding.Unicode.WebName,
			SnackbarHostIdentifiers.Notepad,
			"tab 1");

		// Assert
		result
			.Should()
			.BeTrue();

		editor
			.Received(1)
			.ReplaceText(Encoding.Unicode.GetString(Encoding.UTF8.GetBytes("Text")));
	}

	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: the text read in another encoding is not an edit, so the editor
	/// replaces its text, which undo starts from, and the document gets no edit.
	/// </summary>
	[Test]
	public async Task SwitchAsync_Replaces_The_Text_Without_An_Edit()
	{
		// Arrange
		IEncodedDocument editor = Substitute.For<IEncodedDocument>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			DocumentCodec codec = new();

			codec.Read([], Encoding.UTF8.WebName);

			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Text
				.Returns("Text");

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		// Act
		await sut.SwitchAsync(
			editor,
			Encoding.Unicode.WebName,
			SnackbarHostIdentifiers.Notepad,
			"tab 1");

		// Assert
		editor
			.Received(1)
			.ReplaceText(Encoding.Unicode.GetString(Encoding.UTF8.GetBytes("Text")));

		editor
			.Document
			.DidNotReceive()
			.Replace(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: a text that its encoding cannot hold keeps the encoding with a
	/// warning in the window of the editor.
	/// </summary>
	[Test]
	public async Task SwitchAsync_Tells_In_The_Window_That_The_Text_Does_Not_Fit_Its_Encoding()
	{
		// Arrange
		INotificationService notification = Substitute.For<INotificationService>();

		IEncodedDocument editor = Substitute.For<IEncodedDocument>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			DocumentCodec codec = new();

			codec.Read(Encoding.UTF8.GetBytes("Text"), Encoding.UTF8.WebName);

			editor
				.Codec
				.Returns(codec);

			// A lone surrogate, which no encoding holds.
			editor
				.Document
				.Text
				.Returns("Text\uD800");

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(notification);
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		// Act
		await sut.SwitchAsync(
			editor,
			Encoding.Unicode.WebName,
			SnackbarHostIdentifiers.Notepad,
			"tab 1");

		// Assert
		notification
			.Received(1)
			.ShowWarningSnackbar(Arg.Any<string>(), Arg.Is(SnackbarHostIdentifiers.Notepad));
	}

	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: an encoding that cannot read the text is refused with a warning
	/// in the window of the editor.
	/// </summary>
	[Test]
	public async Task SwitchAsync_Tells_In_The_Window_Why_The_Choice_Cannot_Read_The_Text()
	{
		// Arrange
		INotificationService notification = Substitute.For<INotificationService>();

		IEncodedDocument editor = Substitute.For<IEncodedDocument>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			DocumentCodec codec = new();

			codec.Read(Encoding.UTF8.GetBytes("Tex"), Encoding.UTF8.WebName);

			editor
				.Codec
				.Returns(codec);

			editor
				.Document
				.Text
				.Returns("Tex");

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(notification);
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		// Act
		await sut.SwitchAsync(
			editor,
			Encoding.Unicode.WebName,
			SnackbarHostIdentifiers.Main,
			"file");

		// Assert
		notification
			.Received(1)
			.ShowWarningSnackbar(Arg.Any<string>(), Arg.Is(SnackbarHostIdentifiers.Main));
	}
	#endregion
}
