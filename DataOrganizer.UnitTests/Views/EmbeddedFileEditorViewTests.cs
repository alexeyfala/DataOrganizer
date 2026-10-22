using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using AwesomeAssertions;
using DataOrganizer.Controls.Text;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Text;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services.Documents;
using DataOrganizer.Services.Text;
using DataOrganizer.Services.Views;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(EmbeddedFileEditorView)}"" type")]
internal class EmbeddedFileEditorViewTests
{
	#region Data
	/// <summary>
	/// Name of a PowerShell script.
	/// </summary>
	private const string PowerShellFileName = "script.ps1";

	/// <summary>
	/// Language of <see cref="PowerShellFileName" />.
	/// </summary>
	private const string PowerShellLanguage = "powershell";

	/// <summary>
	/// A line of PowerShell.
	/// </summary>
	private const string PowerShellText = "if ($value) { Write-Host 'Text' }";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DocumentEditorViewModel.DefaultEncoding" />: the encoding found in the bytes of the file reaches
	/// the editor, apart from the one chosen for its text.
	/// </summary>
	[AvaloniaTest]
	public async Task DefaultEncoding_Reaches_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = Encoding.UTF8.GetBytes("Hi!"),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			// The stored choice tells the encoding of the text from the one found in its bytes.
			FileEditorState state = new()
			{
				Encoding = "cp866"
			};

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns(new SystemTextJsonSerializer().Serialize(state));

			builder
				.RegisterType<SystemTextJsonSerializer>()
				.As<IJsonSerializer>();

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<EditorStateStore>()
				.As<IEditorStateStore>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		await viewModel.EditorLoaded();

		// Act
		using EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut.GetLogicalDescendants().OfType<DocumentEditorView>().Single().DefaultEncoding
			.Should()
			.Be("utf-8");
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.DefaultSyntaxLanguage" />: the language of the file extension reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public async Task DefaultSyntaxLanguage_Reaches_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(PowerShellText),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterInstance(SyntaxRegistry.Instance)
				.As<ISyntaxRegistry>();

			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(fileName: PowerShellFileName));

		await viewModel.EditorLoaded();

		// Act
		using EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut
			.GetLogicalDescendants()
			.OfType<DocumentEditorView>()
			.Single()
			.DefaultSyntaxLanguage
			.Should()
			.Be(PowerShellLanguage);
	}

	/// <summary>
	/// <see cref="DocumentEditorView.DefaultSyntaxLanguageMark" />: the list of the languages marks the one that the
	/// extension of the file gives.
	/// </summary>
	[AvaloniaTest]
	public void DefaultSyntaxLanguageMark_Is_Set()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		// Act
		using EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut.GetLogicalDescendants().OfType<DocumentEditorView>().Single().DefaultSyntaxLanguageMark
			.Should()
			.NotBeNullOrEmpty();
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorView.Dispose" />: the highlighting of the editor goes away.
	/// </summary>
	[AvaloniaTest]
	public async Task Dispose_Removes_The_Highlighting()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(PowerShellText),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(fileName: PowerShellFileName));

		await viewModel.EditorLoaded();

		EmbeddedFileEditorView sut = new(viewModel);

		// Act
		sut.Dispose();

		// Assert
		HasHighlighting(sut
			.GetLogicalDescendants()
			.OfType<DocumentTextEditor>()
			.Single())
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.Document" />: the text of the file reaches the editor once it is loaded.
	/// </summary>
	[AvaloniaTest]
	public async Task Document_Reaches_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = Encoding.UTF8.GetBytes("Hello"),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		// The file is loaded once its editor is shown.
		using EmbeddedFileEditorView sut = new(viewModel);

		// Act
		await viewModel.EditorLoaded();

		// Assert
		sut.Editor.Document!.Text
			.Should()
			.Be("Hello");
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.Document" />: an edit in the editor reaches the text of the file, and undo takes
	/// it back as far as the loaded text.
	/// </summary>
	[AvaloniaTest]
	public async Task Document_Shares_The_Edits_And_The_Undo_With_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = Encoding.UTF8.GetBytes("Hello"),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			// An edit starts a pause in typing, which the test does not wait for.
			builder.RegisterInstance(Substitute.For<ITypingPauseFactory>());

			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		using EmbeddedFileEditorView sut = new(viewModel);

		await viewModel.EditorLoaded();

		TextDocument document = sut.Editor.Document!;

		// Act
		document.Insert(5, ", world");

		string edited = viewModel.Editor.Document.Text;

		document.UndoStack.Undo();

		// Assert
		edited
			.Should()
			.Be("Hello, world");

		viewModel.Editor.Document.Text
			.Should()
			.Be("Hello");

		document.UndoStack.CanUndo
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.Encoding" />: an encoding chosen in the editor comes back to the view
	/// model.
	/// </summary>
	[AvaloniaTest]
	public void Encoding_Follows_A_Choice_In_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		using EmbeddedFileEditorView sut = new(viewModel);

		// Act
		sut
			.GetLogicalDescendants()
			.OfType<DocumentEditorView>()
			.Single()
			.SetCurrentValue(DocumentEditorView.EncodingProperty, "cp866");

		// Assert
		viewModel.Editor.Encoding
			.Should()
			.Be("cp866");
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.Encoding" />: the encoding of the text reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void Encoding_Reaches_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.Editor.Encoding = "cp866";

		// Act
		using EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut.GetLogicalDescendants().OfType<DocumentEditorView>().Single().Encoding
			.Should()
			.Be("cp866");
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.Encoding" />: an encoding chosen in the status bar reads the text of the file
	/// again from the same bytes.
	/// </summary>
	[AvaloniaTest]
	public async Task EncodingBlock_Reads_The_Text_Again_In_The_Chosen_Encoding()
	{
		// Arrange
		const string text = "Привет, мир";

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				// Not UTF-8, so read in the fallback code page first
				Contents = CodePagesEncodingProvider.Instance.GetEncoding(866)!.GetBytes(text),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			// The choice of an encoding is posted to the UI thread.
			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<DocumentEncodingSwitcher>()
				.As<IDocumentEncodingSwitcher>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		await viewModel.EditorLoaded();

		using EmbeddedFileEditorView sut = new(viewModel);

		Window window = new()
		{
			Content = sut,
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		ChoiceSelector encodings = GetEncodingBlock(sut);

		// Act
		encodings.SetCurrentValue(ChoiceSelector.SelectedChoiceProperty, "cp866");

		await RunJobsUntilAsync(() => sut.Editor.Document?.Text == text);

		// Assert
		sut.Editor.Document!.Text
			.Should()
			.Be(text);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.Encoding" />: an encoding that cannot read the text gives way in the status bar
	/// to the one the text is in: here UTF-16 for an odd number of bytes.
	/// </summary>
	[AvaloniaTest]
	public async Task EncodingBlock_Takes_Back_A_Choice_That_Cannot_Read_The_Text()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = Encoding.UTF8.GetBytes("Hi!"),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			// The choice of an encoding is posted to the UI thread.
			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<DocumentEncodingSwitcher>()
				.As<IDocumentEncodingSwitcher>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		await viewModel.EditorLoaded();

		using EmbeddedFileEditorView sut = new(viewModel);

		Window window = new()
		{
			Content = sut,
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		ChoiceSelector encodings = GetEncodingBlock(sut);

		// Act
		encodings.SetCurrentValue(ChoiceSelector.SelectedChoiceProperty, Encoding.Unicode.WebName);

		await RunJobsUntilAsync(() => encodings.SelectedChoice == Encoding.UTF8.WebName);

		// Assert
		encodings.SelectedChoice
			.Should()
			.Be(Encoding.UTF8.WebName);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.Encoding" />: a text that its encoding cannot hold has no bytes to read again,
	/// so the status bar gives the choice up for the encoding the text is in.
	/// </summary>
	[AvaloniaTest]
	public async Task EncodingBlock_Takes_Back_A_Choice_While_The_Text_Has_No_Bytes_In_Its_Encoding()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				// Not UTF-8, so in the fallback code page, which has no emoji
				Contents = [0xC0, 0xC1, 0xC2],
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			// The choice of an encoding is posted to the UI thread.
			builder.RegisterInstance<IDispatcher>(Dispatcher.UIThread);

			builder
				.RegisterType<DispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder
				.RegisterType<DocumentEncodingSwitcher>()
				.As<IDocumentEncodingSwitcher>();

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			// An edit starts a pause in typing, which the test does not wait for.
			builder.RegisterInstance(Substitute.For<ITypingPauseFactory>());

			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		await viewModel.EditorLoaded();

		viewModel.Editor.Document.AsTextDocument().Text += "😀";

		using EmbeddedFileEditorView sut = new(viewModel);

		Window window = new()
		{
			Content = sut,
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		ChoiceSelector encodings = GetEncodingBlock(sut);

		// Act
		encodings.SetCurrentValue(ChoiceSelector.SelectedChoiceProperty, "cp866");

		Dispatcher.UIThread.RunJobs();

		// Assert
		encodings.SelectedChoice
			.Should()
			.Be(FileTextCodec.Fallback.WebName);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.EncodingName" />: the name of the encoding of the file reaches the
	/// editor.
	/// </summary>
	[AvaloniaTest]
	public async Task EncodingName_Reaches_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = Encoding.UTF8.GetBytes("Hi!"),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		await viewModel.EditorLoaded();

		// Act
		using EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut.GetLogicalDescendants().OfType<DocumentEditorView>().Single().EncodingName
			.Should()
			.Be("UTF-8");
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.FindUnreadableEncodings" />: the list of encodings opens with the ones that
	/// cannot read the file already marked, as they are found when it opens: here UTF-16 for an odd number of bytes.
	/// </summary>
	[AvaloniaTest]
	public async Task FindUnreadableEncodings_Marks_The_Encoding_List_As_It_Opens()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = Encoding.UTF8.GetBytes("Hi!"),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<FileContentsOpener>()
				.As<IFileContentsOpener>();

			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		await viewModel.EditorLoaded();

		using EmbeddedFileEditorView sut = new(viewModel);

		Window window = new()
		{
			Content = sut,
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		ChoiceSelector encodings = sut
			.GetLogicalDescendants()
			.OfType<ChoiceSelector>()
			.Single(static x => x.Name == "EncodingBlock");

		Button button = encodings.GetControl<Button>("CurrentChoice");

		// Act
		button.Flyout!.ShowAt(button);

		Dispatcher.UIThread.RunJobs();

		// Assert
		ListBox list = encodings.GetControl<ListBox>("ChoicesList");

		list.Items.Cast<SelectorChoice>().Where(static x => !x.IsAvailable).Select(static x => x.Id)
			.Should()
			.Contain(Encoding.Unicode.WebName);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.FontSize" />: a font size changed in the editor comes back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void FontSize_Follows_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		using EmbeddedFileEditorView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.DocumentFontSizeProperty, 20.0);

		// Assert
		viewModel.Editor.FontSize
			.Should()
			.Be(20.0);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.FontSize" />: the font size of the file reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void FontSize_Reaches_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.Editor.FontSize = 20.0;

		// Act
		using EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut.Editor.DocumentFontSize
			.Should()
			.Be(20.0);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.IsEditingEnabled" />: the text is shown read-only while the read-only mode
	/// is on, and editable while it is off.
	/// </summary>
	[AvaloniaTest]
	public async Task IsEditingEnabled_Reaches_The_Editor([Values] bool isReadOnly)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = Encoding.UTF8.GetBytes("Hello"),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		await viewModel.EditorLoaded();

		viewModel.IsReadOnly = isReadOnly;

		// Act
		using EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut.Editor.IsReadOnly
			.Should()
			.Be(isReadOnly);
	}

	/// <summary>
	/// <see cref="EmbeddedEditorViewModelBase.IsEncrypted" />: the text of an encrypted file reaches the editor
	/// as sensitive.
	/// </summary>
	[AvaloniaTest]
	public void IsEncrypted_Reaches_The_Editor([Values] bool isEncrypted)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(keeperId: isEncrypted ? Guid.NewGuid() : null));

		// Act
		EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut
			.GetLogicalDescendants()
			.OfType<DocumentEditorView>()
			.Single()
			.IsSensitive
			.Should()
			.Be(isEncrypted);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.IsSplit" />: a split made in the editor comes back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Follows_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		using EmbeddedFileEditorView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.IsSplitProperty, true);

		// Assert
		viewModel.Editor.IsSplit
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.IsSplit" />: the split of the file reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Reaches_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.Editor.IsSplit = true;

		// Act
		using EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut.Editor.IsSplit
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ShowEndOfLine" />: line endings shown in the editor come back to the view
	/// model.
	/// </summary>
	[AvaloniaTest]
	public void ShowEndOfLine_Follows_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		using EmbeddedFileEditorView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.ShowEndOfLineProperty, true);

		// Assert
		viewModel.Editor.ShowEndOfLine
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ShowEndOfLine" />: the line endings of the file are shown in the editor.
	/// </summary>
	[AvaloniaTest]
	public void ShowEndOfLine_Reaches_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.Editor.ShowEndOfLine = true;

		// Act
		using EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut.Editor.ShowEndOfLine
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ShowSpaces" />: spaces shown in the editor come back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void ShowSpaces_Follows_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		using EmbeddedFileEditorView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.ShowSpacesProperty, true);

		// Assert
		viewModel.Editor.ShowSpaces
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ShowSpaces" />: the spaces of the file are shown in the editor.
	/// </summary>
	[AvaloniaTest]
	public void ShowSpaces_Reaches_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.Editor.ShowSpaces = true;

		// Act
		using EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut.Editor.ShowSpaces
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ShowTabs" />: tabs shown in the editor come back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void ShowTabs_Follows_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		using EmbeddedFileEditorView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.ShowTabsProperty, true);

		// Assert
		viewModel.Editor.ShowTabs
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ShowTabs" />: the tabs of the file are shown in the editor.
	/// </summary>
	[AvaloniaTest]
	public void ShowTabs_Reaches_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.Editor.ShowTabs = true;

		// Act
		using EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut.Editor.ShowTabs
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.SplitShare" />: a share of the upper half changed in the editor comes back
	/// to the view model.
	/// </summary>
	[AvaloniaTest]
	public void SplitShare_Follows_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		using EmbeddedFileEditorView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.SplitShareProperty, 0.3);

		// Assert
		viewModel.Editor.SplitShare
			.Should()
			.Be(0.3);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.SplitShare" />: the share of the upper half of the file reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void SplitShare_Reaches_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.Editor.SplitShare = 0.3;

		// Act
		using EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut.Editor.SplitShare
			.Should()
			.Be(0.3);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.SyntaxLanguage" />: a language chosen in the editor comes back to the view model.
	/// </summary>
	[AvaloniaTest]
	public async Task SyntaxLanguage_Follows_A_Choice_In_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(PowerShellText),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(fileName: PowerShellFileName));

		await viewModel.EditorLoaded();

		using EmbeddedFileEditorView sut = new(viewModel);

		// Act
		sut
			.GetLogicalDescendants()
			.OfType<DocumentEditorView>()
			.Single()
			.SetCurrentValue(DocumentEditorView.SyntaxLanguageProperty, "bat");

		// Assert
		viewModel.Editor.SyntaxLanguage
			.Should()
			.Be("bat");
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.SyntaxLanguage" />: the language of the file reaches the text editor.
	/// </summary>
	[AvaloniaTest]
	public async Task SyntaxLanguage_Reaches_The_Text_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			ValidatedContents fileContents = new()
			{
				Contents = TextDefaults.Encoding.GetBytes(PowerShellText),
				IsValid = true
			};

			dbAccess
				.GetFileContentsAsync(Arg.Any<Guid>())
				.Returns(fileContents);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FileContentsReader>()
				.As<IFileContentsReader>();

			builder
				.RegisterInstance(SyntaxRegistry.Instance)
				.As<ISyntaxRegistry>();

			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.Initialize(EmbeddedEditorSetupFactory.CreateSetup(fileName: PowerShellFileName));

		await viewModel.EditorLoaded();

		// Act
		using EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut
			.GetLogicalDescendants()
			.OfType<DocumentTextEditor>()
			.Single()
			.SyntaxLanguage
			.Should()
			.Be(PowerShellLanguage);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ViewState" />: the caret, the selection and the scroll position reported by
	/// the editor come back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Follows_The_Editor()
	{
		// Arrange
		DocumentViewState state = new()
		{
			CaretPosition = new()
			{
				Column = 1,
				Line = 3,
				VisualColumn = -1
			},
			ScrollOffset = new()
			{
				X = 0.0,
				Y = 100.0
			},
			SelectionLength = 4,
			SelectionStart = 20
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		using EmbeddedFileEditorView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.ViewStateProperty, state);

		// Assert
		viewModel.Editor.ViewState
			.Should()
			.Be(state);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ViewState" />: the caret, the selection and the scroll position of the file
	/// reach the editor.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Reaches_The_Editor()
	{
		// Arrange
		DocumentViewState state = new()
		{
			CaretPosition = new()
			{
				Column = 1,
				Line = 3,
				VisualColumn = -1
			},
			ScrollOffset = new()
			{
				X = 0.0,
				Y = 100.0
			},
			SelectionLength = 4,
			SelectionStart = 20
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.Editor.ViewState = state;

		// Act
		using EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut.Editor.ViewState
			.Should()
			.Be(state);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.WordWrap" />: long lines wrapped in the editor come back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void WordWrap_Follows_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		using EmbeddedFileEditorView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.WordWrapProperty, true);

		// Assert
		viewModel.Editor.WordWrap
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.WordWrap" />: the long lines of the file are wrapped in the editor.
	/// </summary>
	[AvaloniaTest]
	public void WordWrap_Reaches_The_Editor()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<DocumentTextFactory>()
				.As<IDocumentTextFactory>();
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.Editor.WordWrap = true;

		// Act
		using EmbeddedFileEditorView sut = new(viewModel);

		// Assert
		sut.Editor.WordWrap
			.Should()
			.BeTrue();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the encoding block of the status bar of an editor.
	/// </summary>
	private static ChoiceSelector GetEncodingBlock(EmbeddedFileEditorView view)
	{
		return view
			.GetLogicalDescendants()
			.OfType<ChoiceSelector>()
			.Single(static x => x.Name == "EncodingBlock");
	}

	/// <summary>
	/// <c>True</c> when the editor has the syntax highlighting.
	/// </summary>
	private static bool HasHighlighting(TextEditor editor)
	{
		return editor
			.TextArea
			.TextView
			.LineTransformers
			.OfType<TextMateColoringTransformer>()
			.Any();
	}

	/// <summary>
	/// Runs the jobs of the UI thread until <paramref name="condition" /> holds or the wait runs out.
	/// </summary>
	private static async Task RunJobsUntilAsync(Func<bool> condition)
	{
		Func<bool> isMet = () =>
		{
			Dispatcher.UIThread.RunJobs();

			return condition();
		};

		await isMet.WaitAsync(millisecondsDelay: 10, maxRepeats: 300);
	}
	#endregion
}
