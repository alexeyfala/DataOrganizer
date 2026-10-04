using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using AwesomeAssertions;
using DataOrganizer.Controls;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
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
	/// <see cref="EmbeddedFileEditorViewModel.DefaultSyntaxLanguage" />: the language of the file extension reaches the editor.
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.FileName = PowerShellFileName;

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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.FileName = PowerShellFileName;

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
	/// <see cref="EmbeddedFileEditorViewModel.FindUnreadableEncodings" />: the list of encodings opens with the ones that
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);
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
	/// <see cref="EmbeddedEditorViewModelBase.IsEncrypted" />: the text of an encrypted file reaches the editor
	/// as sensitive.
	/// </summary>
	[AvaloniaTest]
	public void IsEncrypted_Reaches_The_Editor([Values] bool isEncrypted)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.KeeperId = isEncrypted ? Guid.NewGuid() : null;

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
	/// <see cref="EmbeddedFileEditorViewModel.SyntaxLanguage" />: a language chosen in the editor comes back to the view model.
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.FileName = PowerShellFileName;

		await viewModel.EditorLoaded();

		using EmbeddedFileEditorView sut = new(viewModel);

		// Act
		sut
			.GetLogicalDescendants()
			.OfType<DocumentEditorView>()
			.Single()
			.SetCurrentValue(DocumentEditorView.SyntaxLanguageProperty, "bat");

		// Assert
		viewModel.SyntaxLanguage
			.Should()
			.Be("bat");
	}

	/// <summary>
	/// <see cref="EmbeddedFileEditorViewModel.SyntaxLanguage" />: the language of the file reaches the text editor.
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

			dbAccess
				.GetFileEditorStateAsync(Arg.Any<Guid>())
				.Returns((string?)null);

			builder.RegisterInstance(dbAccess);
		});

		using EmbeddedFileEditorViewModel viewModel = mock.Create<EmbeddedFileEditorViewModel>();

		viewModel.FileName = PowerShellFileName;

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
	#endregion

	#region Helpers
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
	#endregion
}
