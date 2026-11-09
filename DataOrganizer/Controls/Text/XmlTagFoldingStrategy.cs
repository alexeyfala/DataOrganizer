using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TextProcessing.Dto;
using TextProcessing.Helpers;

namespace DataOrganizer.Controls.Text;

/// <summary>
/// <see cref="IFoldingStrategy" /> of XML, which folds the elements and the comments of several lines by their tags,
/// with the strategy of the engine, and the regions between the markers of the language.
/// </summary>
internal sealed class XmlTagFoldingStrategy : IFoldingStrategy
{
	#region Data
	/// <summary>
	/// Token that closes a comment of XML.
	/// </summary>
	private const string CommentEnd = "-->";

	/// <summary>
	/// Token that opens a comment of XML.
	/// </summary>
	private const string CommentStart = "<!--";

	/// <summary>
	/// Strategy of the regions, which joins them with the blocks of the engine.
	/// </summary>
	private readonly CompositeFoldingStrategy _regionStrategy;

	/// <summary>
	/// Strategy of the engine, which reads the text as XML up to its first error.
	/// </summary>
	private readonly XmlFoldingStrategy _tagStrategy = new();
	#endregion

	#region Constructors
	public XmlTagFoldingStrategy(SyntaxFoldingRules rules)
	{
		_regionStrategy = new CompositeFoldingStrategy(new MarkerFoldingStrategy(rules));
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
	{
		string text = document.Text;

		// A document type is skipped, so it neither stops the reading nor loads its definition.
		XmlReaderSettings settings = new()
		{
			DtdProcessing = DtdProcessing.Ignore
		};

		using XmlReader reader = XmlReader.Create(document.CreateReader(), settings);

		TextFolding[] tagBlocks = [.. _tagStrategy
			.CreateNewFoldings(
				document,
				reader,
				out firstErrorOffset)
			.Select(x => ToTextFolding(
				document,
				text,
				x))];

		// The manager keeps its blocks after an error, which a new block there would push out.
		int readEnd = firstErrorOffset < 0 ? int.MaxValue : firstErrorOffset;

		return [.. _regionStrategy
			.CreateNewFoldings(text, tagBlocks)
			.TakeWhile(x => x.StartOffset < readEnd)
			.Select(static x => x.ToNewFolding())];
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns a block of the engine as a block of the text: a comment up to its end token with the banner of its first
	/// line, as the comments of other languages show it, and an element as the engine found it.
	/// </summary>
	private static TextFolding ToTextFolding(
		TextDocument document,
		string text,
		NewFolding folding)
	{
		ReadOnlySpan<char> rest = text.AsSpan(folding.StartOffset);

		if (!rest.StartsWith(CommentStart, StringComparison.Ordinal))
		{
			return new TextFolding
			{
				EndOffset = folding.EndOffset,
				Name = folding.Name,
				StartOffset = folding.StartOffset
			};
		}

		int lineEnd = document
			.GetLineByOffset(folding.StartOffset)
			.EndOffset;

		// The engine measures a comment by its value, where the reader turns each "\r\n" into one character.
		int end = rest[CommentStart.Length..].IndexOf(CommentEnd, StringComparison.Ordinal);

		return new TextFolding
		{
			EndOffset = folding.StartOffset + CommentStart.Length + end + CommentEnd.Length,
			Name = CommentBanner.Create(rest[..(lineEnd - folding.StartOffset)]),
			StartOffset = folding.StartOffset
		};
	}
	#endregion
}
