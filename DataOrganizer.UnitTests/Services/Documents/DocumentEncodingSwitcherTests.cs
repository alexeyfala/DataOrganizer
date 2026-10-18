using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Services.Documents;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.ViewModels;
using NSubstitute;
using System.Text;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Documents;

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

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(notification);
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		DocumentEditorViewModel editor = new(new AvaloniaDocumentText());

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

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(notification);
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		DocumentEditorViewModel editor = DocumentEditorFactory.CreateEditor(
			Encoding.UTF8.GetBytes("Text"),
			Encoding.UTF8.WebName);

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
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		DocumentEditorViewModel editor = DocumentEditorFactory.CreateEditor(
			Encoding.UTF8.GetBytes("Tex"),
			Encoding.UTF8.WebName);

		// The choice as a binding writes it.
		editor.Encoding = Encoding.Unicode.WebName;

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

		editor.Encoding
			.Should()
			.Be(Encoding.UTF8.WebName);
	}

	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: a text that its encoding cannot hold has no bytes to read again,
	/// so the encoding stays.
	/// </summary>
	[Test]
	public async Task SwitchAsync_Gives_Way_When_The_Text_Has_No_Bytes_In_Its_Encoding()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		DocumentEditorViewModel editor = DocumentEditorFactory.CreateEditor(
			Encoding.UTF8.GetBytes("Text"),
			Encoding.UTF8.WebName);

		// A lone surrogate, which no encoding holds.
		editor.Document.AsTextDocument().Text = "Text\uD800";

		// The choice as a binding writes it.
		editor.Encoding = Encoding.Unicode.WebName;

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

		editor.Encoding
			.Should()
			.Be(Encoding.UTF8.WebName);
	}

	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: an encoding that cannot read the text leaves the text as it
	/// was.
	/// </summary>
	[Test]
	public async Task SwitchAsync_Keeps_The_Text_When_The_Choice_Cannot_Read_It()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		DocumentEditorViewModel editor = DocumentEditorFactory.CreateEditor(
			Encoding.UTF8.GetBytes("Tex"),
			Encoding.UTF8.WebName);

		// Act
		await sut.SwitchAsync(
			editor,
			Encoding.Unicode.WebName,
			SnackbarHostIdentifiers.Main,
			"file");

		// Assert
		editor.Document.Text
			.Should()
			.Be("Tex");
	}

	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: the text stays as it is until the posted work runs, after the
	/// binding that wrote the choice is done.
	/// </summary>
	[Test]
	public void SwitchAsync_Leaves_The_Text_Until_The_Posted_Work_Runs()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		DocumentEditorViewModel editor = DocumentEditorFactory.CreateEditor(
			Encoding.UTF8.GetBytes("Text"),
			Encoding.UTF8.WebName);

		// Act
		_ = sut.SwitchAsync(
			editor,
			Encoding.Unicode.WebName,
			SnackbarHostIdentifiers.Main,
			"file");

		// Assert
		editor.Document.Text
			.Should()
			.Be("Text");
	}

	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: another encoding reads the text again from the same bytes and
	/// answers so.
	/// </summary>
	[Test]
	public async Task SwitchAsync_Reads_The_Text_Again_In_The_Chosen_Encoding()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		DocumentEditorViewModel editor = DocumentEditorFactory.CreateEditor(
			Encoding.UTF8.GetBytes("Text"),
			Encoding.UTF8.WebName);

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

		editor.Document.Text
			.Should()
			.Be(Encoding.Unicode.GetString(Encoding.UTF8.GetBytes("Text")));
	}

	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: the editor shows the name of the encoding its text is read in
	/// again.
	/// </summary>
	[Test]
	public async Task SwitchAsync_Shows_The_Chosen_Encoding()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		DocumentEditorViewModel editor = DocumentEditorFactory.CreateEditor(
			Encoding.UTF8.GetBytes("Text"),
			Encoding.UTF8.WebName);

		// Act
		await sut.SwitchAsync(
			editor,
			Encoding.Unicode.WebName,
			SnackbarHostIdentifiers.Notepad,
			"tab 1");

		// Assert
		editor.EncodingName
			.Should()
			.Be("UTF-16 LE");
	}

	/// <summary>
	/// <see cref="DocumentEncodingSwitcher.SwitchAsync" />: the text read in another encoding is not an edit, so undo
	/// starts from it.
	/// </summary>
	[Test]
	public async Task SwitchAsync_Starts_The_Undo_From_The_Text_Read_Again()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		DocumentEditorViewModel editor = DocumentEditorFactory.CreateEditor(
			[],
			Encoding.UTF8.WebName);

		editor.Document.AsTextDocument().Text = "Text";

		// Act
		await sut.SwitchAsync(
			editor,
			Encoding.Unicode.WebName,
			SnackbarHostIdentifiers.Notepad,
			"tab 1");

		// Assert
		editor.Document.AsTextDocument().UndoStack.CanUndo
			.Should()
			.BeFalse();
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

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(notification);
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		DocumentEditorViewModel editor = DocumentEditorFactory.CreateEditor(
			Encoding.UTF8.GetBytes("Text"),
			Encoding.UTF8.WebName);

		// A lone surrogate, which no encoding holds.
		editor.Document.AsTextDocument().Text = "Text\uD800";

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

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance(notification);
		});

		DocumentEncodingSwitcher sut = mock.Create<DocumentEncodingSwitcher>();

		DocumentEditorViewModel editor = DocumentEditorFactory.CreateEditor(
			Encoding.UTF8.GetBytes("Tex"),
			Encoding.UTF8.WebName);

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
