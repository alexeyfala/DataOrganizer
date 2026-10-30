using Avalonia.Data.Converters;
using AwesomeAssertions;
using DataOrganizer.Converters;
using DataOrganizer.Services.Documents;
using EditorDocuments.Dto;
using EditorDocuments.Enums;
using System.Globalization;

namespace DataOrganizer.UnitTests.Converters;

[TestFixture(Description = $@"Tests of ""{nameof(DocumentConverters)}"" type")]
internal class DocumentConvertersTests
{
	#region Methods
	/// <summary>
	/// <see cref="DocumentConverters.LineEndingToCaption" />: line endings of one style are named after their system and characters.
	/// </summary>
	[TestCase(LineEnding.CrLf, "Windows (CR LF)")]
	[TestCase(LineEnding.Lf, "Unix (LF)")]
	[TestCase(LineEnding.Cr, "Macintosh (CR)")]
	public void LineEndingToCaption_Names_The_Line_Break(LineEnding ending, string expected)
	{
		// Act
		object? result = Convert(DocumentConverters.LineEndingToCaption, ending);

		// Assert
		result
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="DocumentConverters.LineEndingToCaption" />: a document without line breaks has no caption.
	/// </summary>
	[Test]
	public void LineEndingToCaption_Without_Line_Breaks_Is_Empty()
	{
		// Act
		object? result = Convert(DocumentConverters.LineEndingToCaption, LineEnding.None);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DocumentConverters.NumberCaption" />: puts the number, with its digits grouped, into the format given as the parameter.
	/// </summary>
	[Test]
	public void NumberCaption_Groups_The_Digits()
	{
		// Act
		object? result = Convert(DocumentConverters.NumberCaption, 1234567, "[{0}]");

		// Assert
		result
			.Should()
			.Be($"[{1234567.ToString("N0", CultureInfo.CurrentCulture)}]");
	}

	/// <summary>
	/// <see cref="DocumentConverters.SelectionOrPosition" />: while nothing is selected, shows the caret position counted from one.
	/// </summary>
	[Test]
	public void SelectionOrPosition_Shows_The_Position_Counted_From_One()
	{
		// Arrange
		DocumentStatus status = default(DocumentStatus) with
		{
			CaretOffset = 776
		};

		// Act
		object? result = Convert(DocumentConverters.SelectionOrPosition, status);

		// Assert
		result
			.Should()
			.BeOfType<string>()
			.Which
			.Should()
			.Contain("777");
	}

	/// <summary>
	/// <see cref="DocumentConverters.SelectionOrPosition" />: a selection shows its characters and lines instead of the caret position.
	/// </summary>
	[Test]
	public void SelectionOrPosition_Shows_The_Selection_Instead_Of_The_Position()
	{
		// Arrange
		DocumentStatus status = default(DocumentStatus) with
		{
			CaretOffset = 776,
			SelectionLength = 128,
			SelectionLineCount = 45
		};

		// Act
		object? result = Convert(DocumentConverters.SelectionOrPosition, status);

		// Assert
		result
			.Should()
			.BeOfType<string>()
			.Which
			.Should()
			.Contain("128")
			.And
			.Contain("45")
			.And
			.NotContain("777");
	}

	/// <summary>
	/// <see cref="DocumentConverters.SyntaxLanguageToName" />: a language takes the name its grammar gives it.
	/// </summary>
	[Test]
	public void SyntaxLanguageToName_Gives_The_Name_Of_The_Grammar()
	{
		// Act
		object? result = Convert(DocumentConverters.SyntaxLanguageToName, "powershell");

		// Assert
		result
			.Should()
			.Be("PowerShell");
	}

	/// <summary>
	/// <see cref="DocumentConverters.SyntaxLanguageToName" />: a language without a grammar is named as plain text.
	/// </summary>
	[Test]
	[TestCase("unknown")]
	[TestCase(FileEditorState.PlainTextLanguage)]
	public void SyntaxLanguageToName_Names_A_Language_Without_A_Grammar_As_Plain_Text(string language)
	{
		// Act
		object? result = Convert(DocumentConverters.SyntaxLanguageToName, language);

		// Assert
		result
			.Should()
			.NotBeNull()
			.And
			.Be(Convert(DocumentConverters.SyntaxLanguageToName, null));
	}

	/// <summary>
	/// <see cref="DocumentConverters.ToTextDocument" />: a text gives the document of the editor that keeps it, and no text
	/// gives none.
	/// </summary>
	[Test]
	public void ToTextDocument_Gives_The_Document_That_Keeps_The_Text()
	{
		// Arrange
		AvaloniaLogText text = new();

		// Act
		object? document = DocumentConverters.ToTextDocument.Convert(
			text,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);

		object? none = DocumentConverters.ToTextDocument.Convert(
			null,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);

		// Assert
		document
			.Should()
			.BeSameAs(text.Document);

		none
			.Should()
			.BeNull();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Runs a converter over the line endings of a document.
	/// </summary>
	private static object? Convert(FuncValueConverter<LineEnding, string?> converter, LineEnding ending)
	{
		return converter.Convert(
			ending,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over a number with the format given as the parameter.
	/// </summary>
	private static object? Convert(FuncValueConverter<int, string, string> converter, int number, string format)
	{
		return converter.Convert(
			number,
			typeof(object),
			format,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over the status of a document.
	/// </summary>
	private static object? Convert(FuncValueConverter<DocumentStatus, string> converter, DocumentStatus status)
	{
		return converter.Convert(
			status,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over a language of the syntax highlighting.
	/// </summary>

	private static object? Convert(FuncValueConverter<string?, string> converter, string? language)
	{
		return converter.Convert(
			language,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}
	#endregion
}
