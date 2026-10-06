using DataOrganizer.Dto.Documents;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Grammars and color themes of the syntax highlighting, read once and shared by all editors.
/// </summary>
internal sealed class SyntaxRegistry : IRegistryOptions
{
	#region Properties
	/// <summary>
	/// The registry of the application.
	/// </summary>
	public static SyntaxRegistry Instance => LazyInstance.Value;

	/// <summary>
	/// Languages that have a grammar, sorted by name, which the extensions of their files find.
	/// </summary>
	public IReadOnlyList<SelectorChoice> Languages { get; }
	#endregion

	#region Data
	/// <summary>
	/// The registry, made on first use, as it reads the list of all grammars.
	/// </summary>
	private static readonly Lazy<SyntaxRegistry> LazyInstance = new(static () => new SyntaxRegistry());

	/// <summary>
	/// Color themes read so far.
	/// </summary>
	private readonly ConcurrentDictionary<ThemeName, IRawTheme> _colorThemes = [];

	/// <summary>
	/// Folding rules built so far, by language, as each builds its regular expressions.
	/// </summary>
	private readonly ConcurrentDictionary<string, SyntaxFoldingRules?> _foldingRules = [];

	/// <summary>
	/// Grammars read so far, by scope name, as every highlighted editor would parse its grammar again.
	/// </summary>
	private readonly ConcurrentDictionary<string, IRawGrammar?> _grammars = [];

	/// <summary>
	/// Grammars, language configurations and color themes that come with TextMate.
	/// </summary>
	private readonly RegistryOptions _options = new(ThemeName.LightPlus);

	/// <summary>
	/// Themes read so far, by name, for the themes that color themes include.
	/// </summary>
	private readonly ConcurrentDictionary<string, IRawTheme?> _themes = [];
	#endregion

	#region Constructors
	private SyntaxRegistry()
	{
		// A language may come with more than one grammar package, as the one of diff does.
		Languages = [.. _options
			.GetAvailableLanguages()
			.DistinctBy(static x => x.Id)
			.Select(static x => new SelectorChoice
			{
				Id = x.Id,
				Name = x.Aliases[0],
				SearchTerms = [.. (x.Extensions ?? []).Select(static y => y.TrimStart('.'))]
			})
			.OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)];
	}
	#endregion

	#region Methods
	/// <summary>
	/// Returns the rules for folding the text of a language; <c>null</c> for a language without a grammar.
	/// </summary>
	public SyntaxFoldingRules? FindFoldingRules(string language) => _foldingRules.GetOrAdd(language, CreateFoldingRules);

	/// <summary>
	/// Returns the language of a file by the extension of its name; <c>null</c> when no grammar knows the extension.
	/// </summary>
	public string? FindLanguage(string? fileName)
	{
		ReadOnlySpan<char> extension = FileNameHelper.GetExtension(fileName);

		if (extension.IsEmpty)
		{
			return null;
		}

		string value = extension.ToString();

		// Only the languages with a grammar, as the others would leave the text plain.
		return _options
			.GetAvailableLanguages()
			.FirstOrDefault(x => x.Extensions?.Contains(value, StringComparer.OrdinalIgnoreCase) == true)
			?.Id;
	}

	/// <summary>
	/// Returns the scope name of the grammar of a language; <c>null</c> for a language without a grammar.
	/// </summary>
	public string? FindScope(string language) => _options.GetScopeByLanguageId(language);

	/// <summary>
	/// Returns the color theme for a dark or a light background.
	/// </summary>
	public IRawTheme GetColorTheme(bool isDark)
	{
		return _colorThemes.GetOrAdd(
			isDark ? ThemeName.DarkPlus : ThemeName.LightPlus,
			_options.LoadTheme);
	}

	/// <inheritdoc />
	public IRawTheme GetDefaultTheme() => GetColorTheme(isDark: false);

	/// <inheritdoc />
	public IRawGrammar? GetGrammar(string scopeName) => _grammars.GetOrAdd(scopeName, _options.GetGrammar);

	/// <inheritdoc />
	public ICollection<string>? GetInjections(string scopeName) => _options.GetInjections(scopeName);

	/// <inheritdoc />
	public IRawTheme? GetTheme(string scopeName) => _themes.GetOrAdd(scopeName, _options.GetTheme);
	#endregion

	#region Helpers
	/// <summary>
	/// Builds the folding rules of a language from its settings and from the patterns of its lines.
	/// </summary>
	private SyntaxFoldingRules? CreateFoldingRules(string language)
	{
		if (_options
			.GetAvailableLanguages()
			.FirstOrDefault(x => x.Id == language) is not { } found)
		{
			return null;
		}

		Comments? comments = found.Configuration?.Comments;

		Folding? folding = found.Configuration?.Folding;

		// A token of blanks, as some languages have for the end, would close a block comment anywhere.
		(string Start, string End)? blockComment = comments?.BlockComment is [var start, var end]
			&& !string.IsNullOrWhiteSpace(start)
			&& !string.IsNullOrWhiteSpace(end)
				? (start, end)
				: null;

		// A marker without its pair would open blocks that never close.
		Markers? markers = folding is { IsEmpty: false, Markers: { } pair } ? pair : null;

		// The markers are regular expressions of VS Code, which .NET reads alike.
		// They are compiled, as every pass tries them on each line.
		return new SyntaxFoldingRules
		{
			BlockCommentEnd = blockComment?.End,
			BlockCommentStart = blockComment?.Start,
			DirectiveLine = SyntaxLinePatterns.FindDirective(language),
			DocComment = SyntaxLinePatterns.FindDocComment(language),
			EndLine = SyntaxLinePatterns.FindEndLine(language),
			EndMarker = markers is null ? null : new Regex(markers.End, RegexOptions.Compiled),
			ImportLine = SyntaxLinePatterns.FindImport(language),
			IsOffSide = folding?.OffSide == true,
			LineComment = SyntaxLinePatterns.FindLineComment(language, comments?.LineComment),
			PreprocessorLine = SyntaxLinePatterns.FindPreprocessor(language),
			StartMarker = markers is null ? null : new Regex(markers.Start, RegexOptions.Compiled)
		};
	}
	#endregion
}
