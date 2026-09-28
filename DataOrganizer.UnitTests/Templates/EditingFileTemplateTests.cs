using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using AwesomeAssertions;
using DataOrganizer.Controls;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Templates;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Templates;

[TestFixture(Description = $@"Tests of ""{nameof(EditingFileTemplate)}"" type")]
internal class EditingFileTemplateTests
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
	/// <see cref="EditingFileTemplate.Build" />: the name of the file gives the language of its text to the editor.
	/// </summary>
	[AvaloniaTest]
	public async Task Build_Gives_The_Language_Of_The_File_To_The_Editor()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateNamedFileDto(PowerShellFileName);

		file.IsEditing = true;

		using AutoMock viewModelMock = AutoMock.GetLoose(builder =>
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

		EmbeddedFileEditorViewModel viewModel = viewModelMock.Create<EmbeddedFileEditorViewModel>();

		using EmbeddedFileEditorView view = new(viewModel);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EmbeddedFileEditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateUserControl<EmbeddedFileEditorView>(Arg.Any<object[]>())
				.Returns(view);

			builder.RegisterInstance(viewFactory);
		});

		EditingFileTemplate sut = mock.Create<EditingFileTemplate>();

		// Act
		sut.Build(file);

		await viewModel.EditorLoaded();

		// Assert
		view
			.GetLogicalDescendants()
			.OfType<DocumentTextEditor>()
			.Single()
			.SyntaxLanguage
			.Should()
			.Be(PowerShellLanguage);
	}

	/// <summary>
	/// <see cref="EditingFileTemplate.Remove" />: the control of a closed file gives up the highlighting of its text.
	/// </summary>
	[AvaloniaTest]
	public async Task Remove_Disposes_The_Control()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateNamedFileDto(PowerShellFileName);

		file.IsEditing = true;

		using AutoMock viewModelMock = AutoMock.GetLoose(builder =>
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

		EmbeddedFileEditorViewModel viewModel = viewModelMock.Create<EmbeddedFileEditorViewModel>();

		using EmbeddedFileEditorView view = new(viewModel);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateViewModel<EmbeddedFileEditorViewModel>()
				.Returns(viewModel);

			viewFactory
				.CreateUserControl<EmbeddedFileEditorView>(Arg.Any<object[]>())
				.Returns(view);

			builder.RegisterInstance(viewFactory);
		});

		EditingFileTemplate sut = mock.Create<EditingFileTemplate>();

		sut.Build(file);

		await viewModel.EditorLoaded();

		// Act
		sut.Remove(file);

		// Assert
		HasHighlighting(view
			.GetLogicalDescendants()
			.OfType<DocumentTextEditor>()
			.Single())
			.Should()
			.BeFalse();
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
