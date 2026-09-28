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
	/// Languages that have a grammar, sorted by name.
	/// </summary>
	public IReadOnlyList<SyntaxLanguageChoice> Languages { get; }
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
			.Select(static x => new SyntaxLanguageChoice
			{
				Extensions = [.. x.Extensions ?? []],
				Id = x.Id,
				Name = x.Aliases[0]
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
	/// Builds the folding rules of a language from its settings.
	/// </summary>
	private SyntaxFoldingRules? CreateFoldingRules(string language)
	{
		if (_options
			.GetAvailableLanguages()
			.FirstOrDefault(x => x.Id == language) is not { } found)
		{
			return null;
		}

		Folding? folding = found.Configuration?.Folding;

		// A marker without its pair would open blocks that never close.
		if (folding is not { IsEmpty: false, Markers: { } markers })
		{
			return new SyntaxFoldingRules
			{
				IsOffSide = folding?.OffSide == true
			};
		}

		// The markers are regular expressions of VS Code, which .NET reads alike.
		return new SyntaxFoldingRules
		{
			EndMarker = new Regex(markers.End),
			IsOffSide = folding.OffSide,
			StartMarker = new Regex(markers.Start)
		};
	}
	#endregion
}
