using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
	}
	#endregion

	#region Methods
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
}
