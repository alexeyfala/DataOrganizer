using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(NotepadTabView)}"" type")]
internal class NotepadTabViewTests
{
	#region Data
	/// <summary>
	/// Language of PowerShell scripts.
	/// </summary>
	private const string PowerShellLanguage = "powershell";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DocumentEditorView.DefaultSyntaxLanguageMark" />: a tab has no file extension, so the list of the
	/// languages marks none as the default.
	/// </summary>
	[AvaloniaTest]
	public void DefaultSyntaxLanguageMark_Is_Not_Set()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.DefaultSyntaxLanguageMark
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.Document" />: the text of the tab reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void Document_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.Document
			.Should()
			.BeSameAs(viewModel.Document);
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.FontSize" />: a font size changed in the editor comes back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void FontSize_Follows_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1
		};

		using NotepadTabView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.DocumentFontSizeProperty, 20.0);

		// Assert
		viewModel.FontSize
			.Should()
			.Be(20.0);
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.FontSize" />: the font size of the tab reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void FontSize_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			FontSize = 20.0,
			Number = 1
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.DocumentFontSize
			.Should()
			.Be(20.0);
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.IsSplit" />: a split made in the editor comes back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Follows_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1
		};

		using NotepadTabView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.IsSplitProperty, true);

		// Assert
		viewModel.IsSplit
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.IsSplit" />: the split of the tab reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			IsSplit = true,
			Number = 1
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.IsSplit
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.ShowEndOfLine" />: line endings shown in the editor come back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void ShowEndOfLine_Follows_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1
		};

		using NotepadTabView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.ShowEndOfLineProperty, true);

		// Assert
		viewModel.ShowEndOfLine
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.ShowEndOfLine" />: the line endings of the tab are shown in the editor.
	/// </summary>
	[AvaloniaTest]
	public void ShowEndOfLine_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1,
			ShowEndOfLine = true
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.ShowEndOfLine
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.ShowSpaces" />: spaces shown in the editor come back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void ShowSpaces_Follows_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1
		};

		using NotepadTabView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.ShowSpacesProperty, true);

		// Assert
		viewModel.ShowSpaces
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.ShowSpaces" />: the spaces of the tab are shown in the editor.
	/// </summary>
	[AvaloniaTest]
	public void ShowSpaces_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1,
			ShowSpaces = true
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.ShowSpaces
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.ShowTabs" />: tabs shown in the editor come back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void ShowTabs_Follows_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1
		};

		using NotepadTabView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.ShowTabsProperty, true);

		// Assert
		viewModel.ShowTabs
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.ShowTabs" />: the tabs of the text are shown in the editor.
	/// </summary>
	[AvaloniaTest]
	public void ShowTabs_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1,
			ShowTabs = true
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.ShowTabs
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.SplitShare" />: a share of the upper half changed in the editor comes back to the view
	/// model.
	/// </summary>
	[AvaloniaTest]
	public void SplitShare_Follows_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1
		};

		using NotepadTabView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.SplitShareProperty, 0.3);

		// Assert
		viewModel.SplitShare
			.Should()
			.Be(0.3);
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.SplitShare" />: the share of the upper half of the tab reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void SplitShare_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1,
			SplitShare = 0.3
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.SplitShare
			.Should()
			.Be(0.3);
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.SyntaxLanguage" />: a language chosen in the editor comes back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void SyntaxLanguage_Follows_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1
		};

		using NotepadTabView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.SyntaxLanguageProperty, PowerShellLanguage);

		// Assert
		viewModel.SyntaxLanguage
			.Should()
			.Be(PowerShellLanguage);
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.SyntaxLanguage" />: the language of the tab reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void SyntaxLanguage_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1,
			SyntaxLanguage = PowerShellLanguage
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.SyntaxLanguage
			.Should()
			.Be(PowerShellLanguage);
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.ViewState" />: the caret, the selection and the scroll position reported by the editor
	/// come back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Follows_The_Editor()
	{
		// Arrange
		DocumentViewState state = new()
		{
			CaretPosition = new(line: 3, column: 1),
			ScrollOffset = new(0.0, 100.0),
			SelectionLength = 4,
			SelectionStart = 20
		};

		NotepadTabViewModel viewModel = new()
		{
			Number = 1
		};

		using NotepadTabView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.ViewStateProperty, state);

		// Assert
		viewModel.ViewState
			.Should()
			.Be(state);
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.ViewState" />: the caret, the selection and the scroll position of the tab reach the
	/// editor.
	/// </summary>
	[AvaloniaTest]
	public void ViewState_Reaches_The_Editor()
	{
		// Arrange
		DocumentViewState state = new()
		{
			CaretPosition = new(line: 3, column: 1),
			ScrollOffset = new(0.0, 100.0),
			SelectionLength = 4,
			SelectionStart = 20
		};

		NotepadTabViewModel viewModel = new()
		{
			Number = 1,
			ViewState = state
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.ViewState
			.Should()
			.Be(state);
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.WordWrap" />: long lines wrapped in the editor come back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void WordWrap_Follows_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1
		};

		using NotepadTabView sut = new(viewModel);

		// Act
		sut
			.Editor
			.SetCurrentValue(DocumentEditorView.WordWrapProperty, true);

		// Assert
		viewModel.WordWrap
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.WordWrap" />: the long lines of the tab are wrapped in the editor.
	/// </summary>
	[AvaloniaTest]
	public void WordWrap_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1,
			WordWrap = true
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.WordWrap
			.Should()
			.BeTrue();
	}
	#endregion
}
