using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using System.Text;

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
	/// <see cref="DocumentEditorViewModel.DefaultEncoding" />: the encoding found in the bytes of the text reaches the editor,
	/// apart from the one chosen for the text.
	/// </summary>
	[AvaloniaTest]
	public void DefaultEncoding_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1
		};

		// Bytes of UTF-8, read in UTF-16 by choice.
		viewModel.Editor.Codec.Read(Encoding.UTF8.GetBytes("Text"), Encoding.Unicode.WebName);

		viewModel.Editor.RefreshEncoding();

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.DefaultEncoding
			.Should()
			.Be(Encoding.UTF8.WebName);
	}

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
	/// <see cref="DocumentEditorViewModel.Document" />: the text of the tab reaches the editor.
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
			.BeSameAs(viewModel.Editor.Document);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.Encoding" />: an encoding chosen in the editor comes back to the view model.
	/// </summary>
	[AvaloniaTest]
	public void Encoding_Follows_The_Editor()
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
		NotepadTabViewModel viewModel = new()
		{
			Editor =
			{
				Encoding = "cp866"
			},
			Number = 1
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.Encoding
			.Should()
			.Be("cp866");
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.EncodingName" />: the name of the encoding of the text reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void EncodingName_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1
		};

		viewModel.Editor.RefreshEncoding();

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.EncodingName
			.Should()
			.Be("UTF-8");
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.FindUnreadableEncodingsCommand" />: the editor finds the encodings that cannot read
	/// the text through the command of the tab.
	/// </summary>
	[AvaloniaTest]
	public void FindUnreadableEncodingsCommand_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.FindUnreadableEncodingsCommand
			.Should()
			.BeSameAs(viewModel.Editor.FindUnreadableEncodingsCommand);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.FontSize" />: a font size changed in the editor comes back to the view model.
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
		viewModel.Editor.FontSize
			.Should()
			.Be(20.0);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.FontSize" />: the font size of the tab reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void FontSize_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Editor =
			{
				FontSize = 20.0
			},
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
	/// <see cref="NotepadTabViewModel.IsReadOnly" />: a text that cannot be read is shown read-only.
	/// </summary>
	[AvaloniaTest]
	public void IsReadOnly_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			IsReadOnly = true,
			Number = 1
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.IsReadOnly
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.IsSplit" />: a split made in the editor comes back to the view model.
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
		viewModel.Editor.IsSplit
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.IsSplit" />: the split of the tab reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void IsSplit_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Editor =
			{
				IsSplit = true
			},
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
	/// <see cref="DocumentEditorViewModel.ShowEndOfLine" />: line endings shown in the editor come back to the view model.
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
		viewModel.Editor.ShowEndOfLine
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ShowEndOfLine" />: the line endings of the tab are shown in the editor.
	/// </summary>
	[AvaloniaTest]
	public void ShowEndOfLine_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Editor =
			{
				ShowEndOfLine = true
			},
			Number = 1
		};

		// Act
		using NotepadTabView sut = new(viewModel);

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
		viewModel.Editor.ShowSpaces
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ShowSpaces" />: the spaces of the tab are shown in the editor.
	/// </summary>
	[AvaloniaTest]
	public void ShowSpaces_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Editor =
			{
				ShowSpaces = true
			},
			Number = 1
		};

		// Act
		using NotepadTabView sut = new(viewModel);

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
		viewModel.Editor.ShowTabs
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ShowTabs" />: the tabs of the text are shown in the editor.
	/// </summary>
	[AvaloniaTest]
	public void ShowTabs_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Editor =
			{
				ShowTabs = true
			},
			Number = 1
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.ShowTabs
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.SplitShare" />: a share of the upper half changed in the editor comes back to the view
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
		viewModel.Editor.SplitShare
			.Should()
			.Be(0.3);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.SplitShare" />: the share of the upper half of the tab reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void SplitShare_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Editor =
			{
				SplitShare = 0.3
			},
			Number = 1
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.SplitShare
			.Should()
			.Be(0.3);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.SyntaxLanguage" />: a language chosen in the editor comes back to the view model.
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
		viewModel.Editor.SyntaxLanguage
			.Should()
			.Be(PowerShellLanguage);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.SyntaxLanguage" />: the language of the tab reaches the editor.
	/// </summary>
	[AvaloniaTest]
	public void SyntaxLanguage_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Editor =
			{
				SyntaxLanguage = PowerShellLanguage
			},
			Number = 1
		};

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.SyntaxLanguage
			.Should()
			.Be(PowerShellLanguage);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.UnreadableEncodings" />: the encodings that cannot read the text reach the editor,
	/// here UTF-16 for an odd number of bytes.
	/// </summary>
	[AvaloniaTest]
	public void UnreadableEncodings_Reach_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Number = 1
		};

		viewModel.Editor.Document.Text = "Hi!";

		viewModel.Editor.FindUnreadableEncodings();

		// Act
		using NotepadTabView sut = new(viewModel);

		// Assert
		sut.Editor.UnreadableEncodings
			.Should()
			.Contain(Encoding.Unicode.WebName);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ViewState" />: the caret, the selection and the scroll position reported by the editor
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
		viewModel.Editor.ViewState
			.Should()
			.Be(state);
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.ViewState" />: the caret, the selection and the scroll position of the tab reach the
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
			Editor =
			{
				ViewState = state
			},
			Number = 1
		};

		// Act
		using NotepadTabView sut = new(viewModel);

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
		viewModel.Editor.WordWrap
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DocumentEditorViewModel.WordWrap" />: the long lines of the tab are wrapped in the editor.
	/// </summary>
	[AvaloniaTest]
	public void WordWrap_Reaches_The_Editor()
	{
		// Arrange
		NotepadTabViewModel viewModel = new()
		{
			Editor =
			{
				WordWrap = true
			},
			Number = 1
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
