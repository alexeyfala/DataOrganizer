using Avalonia.Data.Converters;
using AvaloniaEdit.Document;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Services;
using EditorDocuments.Dto;
using EditorDocuments.Enums;
using EditorDocuments.Extensions;
using Shared.Properties;
using System.Globalization;
using System.Linq;

namespace DataOrganizer.Converters;

/// <summary>
/// Stateless one-way value converters of the texts of documents and of their status, referenced via x:Static.
/// </summary>
internal static class DocumentConverters
{
	#region Properties
	/// <summary>
	/// Caption of the line endings of a document; <c>null</c> for a document without line breaks.
	/// </summary>
	public static FuncValueConverter<LineEnding, string?> LineEndingToCaption { get; } =
		new(static ending => ending.ToCaption());

	/// <summary>
	/// Caption of a number with its digits grouped, put into the format given as the parameter.
	/// </summary>
	public static FuncValueConverter<int, string, string> NumberCaption { get; } = new(GetNumberCaption);

	/// <summary>
	/// Caption of the selection of a document, or of the caret position while nothing is selected.
	/// </summary>
	public static FuncValueConverter<DocumentStatus, string> SelectionOrPosition { get; } = new(GetSelectionOrPosition);

	/// <summary>
	/// Name of a language of the syntax highlighting; the one of plain text for <c>null</c> and for a language without a grammar.
	/// </summary>
	public static FuncValueConverter<string?, string> SyntaxLanguageToName { get; } = new(GetSyntaxLanguageName);

	/// <summary>
	/// Document of the editor that keeps a text.
	/// </summary>
	public static FuncValueConverter<ITextDocumentHolder?, TextDocument?> ToTextDocument { get; } =
		new(static x => x?.Document);
	#endregion

	#region Helpers
	/// <summary>
	/// Formats a number with its digits grouped the way the current culture groups them.
	/// </summary>
	private static string FormatNumber(int number) => number.ToString("N0", CultureInfo.CurrentCulture);

	/// <summary>
	/// Returns a number with its digits grouped, put into a format; the number alone without one.
	/// </summary>
	private static string GetNumberCaption(int number, string? format)
	{
		return string.Format(
			CultureInfo.CurrentCulture,
			format ?? "{0}",
			FormatNumber(number));
	}

	/// <summary>
	/// Returns the caption of the selection of a document, or of the caret position while nothing is selected.
	/// </summary>
	private static string GetSelectionOrPosition(DocumentStatus status)
	{
		if (status.SelectionLength > 0)
		{
			return string.Format(
				CultureInfo.CurrentCulture,
				Strings.SelectedFormat,
				FormatNumber(status.SelectionLength),
				FormatNumber(status.SelectionLineCount));
		}

		return string.Format(
			CultureInfo.CurrentCulture,
			Strings.PositionFormat,
			// The position counts from one, like the line and the column.
			FormatNumber(status.CaretOffset + 1));
	}

	/// <summary>
	/// Returns the name of a language of the syntax highlighting; the one of plain text for <c>null</c> and for a
	/// language without a grammar.
	/// </summary>
	private static string GetSyntaxLanguageName(string? language)
	{
		return SyntaxRegistry
			.Instance
			.Languages
			.FirstOrDefault(x => x.Id == language)
			?.Name ?? Strings.PlainText;
	}
	#endregion
}
