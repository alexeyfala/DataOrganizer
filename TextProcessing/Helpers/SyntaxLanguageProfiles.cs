using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TextProcessing.Dto;
using TextProcessing.Enums;

namespace TextProcessing.Helpers;

/// <summary>
/// The folding rules of each language that its settings lack: how its blocks are found and the patterns of the lines
/// that folding tells apart, such as imports, directives, comments and the ends of blocks.
/// </summary>
public static partial class SyntaxLanguageProfiles
{
	#region Properties
	/// <summary>
	/// Languages that have a profile of their own.
	/// </summary>
	public static IReadOnlyCollection<string> Languages => Profiles.Keys;
	#endregion

	#region Data
	/// <summary>
	/// Profiles by the identifier of the language; the languages of one row share its profile.
	/// </summary>
	private static readonly FrozenDictionary<string, SyntaxFoldingRules> Profiles = CreateProfiles(
		// A remark of Batch is a word in any case or a label that nothing jumps to, beside the token of its settings.
		(["bat"], new()
		{
			LineComment = BatchRemarkRegex()
		}),
		(["c", "cpp", "cuda-cpp", "hlsl", "shaderlab"], new()
		{
			DirectiveLine = DirectiveRegex(),
			ImportLine = CIncludeRegex(),
			PreprocessorLine = CPreprocessorRegex()
		}),
		(["coffeescript", "javascript", "javascriptreact", "typescript", "typescriptreact"], new()
		{
			ImportLine = JavaScriptImportRegex()
		}),
		// A directive of C#, F# and Visual Basic takes one line, so every line with a number sign holds one.
		(["csharp"], new()
		{
			DirectiveLine = DirectiveRegex(),
			DocComment = "///",
			ImportLine = CSharpUsingRegex(),
			PreprocessorLine = DirectiveRegex()
		}),
		(["css", "less", "scss"], new()
		{
			ImportLine = CssImportRegex()
		}),
		(["dart"], new()
		{
			ImportLine = DartImportRegex()
		}),
		(["fsharp"], new()
		{
			DirectiveLine = DirectiveRegex(),
			DocComment = "///",
			ImportLine = FSharpOpenRegex(),
			PreprocessorLine = DirectiveRegex()
		}),
		(["go"], new()
		{
			ImportLine = GoImportRegex()
		}),
		(["groovy", "java"], new()
		{
			ImportLine = JavaImportRegex()
		}),
		(["html"], new()
		{
			EndLine = HtmlEndTagRegex()
		}),
		(["jade"], new()
		{
			ImportLine = PugIncludeRegex()
		}),
		(["julia"], new()
		{
			EndLine = EndWordRegex(),
			ImportLine = JuliaImportRegex()
		}),
		(["latex", "tex"], new()
		{
			EndLine = LatexEndRegex(),
			ImportLine = LatexPackageRegex()
		}),
		(["lua"], new()
		{
			EndLine = EndWordRegex(),
			ImportLine = LuaRequireRegex()
		}),
		(["makefile"], new()
		{
			EndLine = MakefileEndRegex(),
			ImportLine = MakefileIncludeRegex()
		}),
		(["markdown"], new()
		{
			Kind = SyntaxFoldingKind.Headings
		}),
		(["objective-c", "objective-cpp"], new()
		{
			DirectiveLine = DirectiveRegex(),
			ImportLine = ObjectiveCImportRegex(),
			PreprocessorLine = CPreprocessorRegex()
		}),
		(["perl", "perl6"], new()
		{
			ImportLine = PerlUseRegex()
		}),
		// A number sign starts an attribute of PHP and Rust, which may span lines.
		(["php"], new()
		{
			DirectiveLine = DirectiveRegex(),
			ImportLine = PhpUseRegex()
		}),
		(["powershell"], new()
		{
			ImportLine = PowerShellUsingRegex()
		}),
		(["python"], new()
		{
			ImportLine = PythonImportRegex()
		}),
		(["r"], new()
		{
			ImportLine = RLibraryRegex()
		}),
		(["razor"], new()
		{
			EndLine = HtmlEndTagRegex(),
			ImportLine = RazorUsingRegex()
		}),
		(["ruby"], new()
		{
			EndLine = EndWordRegex(),
			ImportLine = RubyRequireRegex()
		}),
		(["rust"], new()
		{
			DirectiveLine = DirectiveRegex(),
			ImportLine = RustUseRegex()
		}),
		(["shellscript"], new()
		{
			EndLine = ShellEndRegex(),
			ImportLine = ShellSourceRegex()
		}),
		(["swift"], new()
		{
			DirectiveLine = DirectiveRegex(),
			ImportLine = SwiftImportRegex(),
			PreprocessorLine = SwiftDirectiveRegex()
		}),
		(["typst"], new()
		{
			ImportLine = TypstImportRegex()
		}),
		(["vb"], new()
		{
			DirectiveLine = DirectiveRegex(),
			DocComment = "'''",
			EndLine = VisualBasicEndRegex(),
			ImportLine = VisualBasicImportsRegex(),
			PreprocessorLine = DirectiveRegex()
		}),
		(["xml", "xsl"], new()
		{
			Kind = SyntaxFoldingKind.Tags
		}));

	/// <summary>
	/// Profile of a language without one of its own: blocks by indentation and no patterns of lines.
	/// </summary>
	private static readonly SyntaxFoldingRules Unlisted = new();
	#endregion

	#region Methods
	/// <summary>
	/// Returns the profile of a language; a language without one gets blocks by indentation and no patterns of lines.
	/// </summary>
	public static SyntaxFoldingRules Find(string language) => Profiles.GetValueOrDefault(language, Unlisted);

	/// <summary>
	/// Returns the pattern of the token that opens a line comment of a language, matched at the start of a text;
	/// <c>null</c> when the language has no token.
	/// </summary>
	public static Regex? FindLineComment(string language, string? token)
	{
		// A pattern of the profile takes in more than the token, as the remarks of Batch do.
		if (Find(language).LineComment is { } pattern)
		{
			return pattern;
		}

		// A token of blanks, as one language has, would find a comment in every text.
		if (string.IsNullOrWhiteSpace(token))
		{
			return null;
		}

		// Compiled, as a pass may try it on each line.
		return new Regex("^" + Regex.Escape(token), RegexOptions.Compiled);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Matches a remark of Batch at the start of a text.
	/// </summary>
	[GeneratedRegex(@"^(?:@?rem\b|::)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex BatchRemarkRegex();

	/// <summary>
	/// Matches a line that includes a header in the languages of the C family.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*#[ \t]*include\b")]
	private static partial Regex CIncludeRegex();

	/// <summary>
	/// Matches a line of a directive of the C family that takes one line, such as a condition or a pragma, but not
	/// a definition, which may go on over the next lines.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*#[ \t]*(?:if(?:n?def)?|elif(?:n?def)?|else|endif|pragma|error|warning|line|undef)\b")]
	private static partial Regex CPreprocessorRegex();

	/// <summary>
	/// Makes the table of the profiles, with the profile of each row under every language of the row.
	/// </summary>
	private static FrozenDictionary<string, SyntaxFoldingRules> CreateProfiles(
		params (string[] Languages, SyntaxFoldingRules Profile)[] rows)
	{
		// A language named in two rows fails here, as one of its profiles would be lost.
		return rows
			.SelectMany(static x => x.Languages, static (x, language) => (Language: language, x.Profile))
			.ToFrozenDictionary(static x => x.Language, static x => x.Profile);
	}

	/// <summary>
	/// Matches a line of a using directive or an extern alias of C#, but not a using statement or declaration.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*(?:(?:global[ \t]+)?using[ \t]+(?:(?:static|unsafe)[ \t]+)*(?:@?\w+[ \t]*=|[\w@.:]+(?:<[^;]*>)?[ \t]*;)|extern[ \t]+alias[ \t]+@?\w+[ \t]*;)")]
	private static partial Regex CSharpUsingRegex();

	/// <summary>
	/// Matches a line that imports a style sheet or a module of styles, but not a variable of Less of the same name.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*@(?:import|use|forward)\b(?![ \t]*:)")]
	private static partial Regex CssImportRegex();

	/// <summary>
	/// Matches a line that imports, exports or joins a library of Dart.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*(?:import|export|part)[ \t]+(?:of\b|['""])")]
	private static partial Regex DartImportRegex();

	/// <summary>
	/// Matches a line that starts with a number sign.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*#")]
	private static partial Regex DirectiveRegex();

	/// <summary>
	/// Matches the text of a line that ends a block of Ruby, Lua or Julia with its word, followed by closing brackets
	/// at most.
	/// </summary>
	[GeneratedRegex(@"^end[)\]},;]*$")]
	private static partial Regex EndWordRegex();

	/// <summary>
	/// Matches a line that opens a module or a namespace of F#.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*open[ \t]+[\w`]")]
	private static partial Regex FSharpOpenRegex();

	/// <summary>
	/// Matches a line that imports a package of Go, alone or in a group.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*import(?:[ \t]+[\w.""(`]|[(""`])")]
	private static partial Regex GoImportRegex();

	/// <summary>
	/// Matches the text of a line that holds end tags alone.
	/// </summary>
	[GeneratedRegex(@"^(?:</[\w:.-]+[ \t]*>[ \t]*)+$")]
	private static partial Regex HtmlEndTagRegex();

	/// <summary>
	/// Matches a line that imports a type or a package of Java or Groovy.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*import[ \t]+[\w.*]")]
	private static partial Regex JavaImportRegex();

	/// <summary>
	/// Matches a line that imports a module of JavaScript or TypeScript, but not a dynamic import or its meta data.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*import(?:[ \t]+[\w{*'""$]|[{*'""])")]
	private static partial Regex JavaScriptImportRegex();

	/// <summary>
	/// Matches a line that uses or imports a module of Julia.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*(?:using|import)[ \t]+[\w.]")]
	private static partial Regex JuliaImportRegex();

	/// <summary>
	/// Matches the text of a line that ends an environment of LaTeX alone.
	/// </summary>
	[GeneratedRegex(@"^\\end\{[^{}]*\}$")]
	private static partial Regex LatexEndRegex();

	/// <summary>
	/// Matches a line that loads a package of LaTeX.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*\\(?:usepackage|RequirePackage)\b")]
	private static partial Regex LatexPackageRegex();

	/// <summary>
	/// Matches a line that requires a module of Lua, also into a variable.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*(?:(?:local[ \t]+)?[\w.]+[ \t]*=[ \t]*)?require[ \t]*[(""']")]
	private static partial Regex LuaRequireRegex();

	/// <summary>
	/// Matches the text of a line that ends a condition or a definition of a makefile.
	/// </summary>
	[GeneratedRegex(@"^(?:endif|endef)$")]
	private static partial Regex MakefileEndRegex();

	/// <summary>
	/// Matches a line that includes another makefile, but not a line of a recipe, which starts with a tab.
	/// </summary>
	[GeneratedRegex(@"^ *(?:-|s)?include[ \t]+\S")]
	private static partial Regex MakefileIncludeRegex();

	/// <summary>
	/// Matches a line that includes or imports a header or a module of Objective-C.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*(?:#[ \t]*(?:include|import)\b|@import[ \t]+\w)")]
	private static partial Regex ObjectiveCImportRegex();

	/// <summary>
	/// Matches a line that uses, drops or requires a module of Perl.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*(?:use|no|require)[ \t]+[\w:'""]")]
	private static partial Regex PerlUseRegex();

	/// <summary>
	/// Matches a line that imports a name or includes a file of PHP.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*(?:use[ \t]+[\\\w]|(?:require|include)(?:_once)?\b)")]
	private static partial Regex PhpUseRegex();

	/// <summary>
	/// Matches a line that uses a namespace, a module or an assembly of PowerShell or imports a module.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*(?:using[ \t]+(?:namespace|module|assembly)[ \t]+\S|import-module\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex PowerShellUsingRegex();

	/// <summary>
	/// Matches a line that includes or extends a template of Pug.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*(?:include|extends)\b")]
	private static partial Regex PugIncludeRegex();

	/// <summary>
	/// Matches a line that imports a module of Python or names from it.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*(?:import[ \t]+[\w.]|from[ \t]+[\w.]+[ \t]+import\b)")]
	private static partial Regex PythonImportRegex();

	/// <summary>
	/// Matches a line that imports a namespace of Razor, but not a using block.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*@using[ \t]+[\w.]")]
	private static partial Regex RazorUsingRegex();

	/// <summary>
	/// Matches a line that loads a package of R.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*(?:library|require)[ \t]*\(")]
	private static partial Regex RLibraryRegex();

	/// <summary>
	/// Matches a line that requires a file of Ruby by its name.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*require(?:_relative)?(?:[ \t]+|[ \t]*\()[ \t]*['""]")]
	private static partial Regex RubyRequireRegex();

	/// <summary>
	/// Matches a line that uses a path of Rust or links an extern crate.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*(?:pub(?:\([^)]*\))?[ \t]+)?(?:use[ \t]+[\w:{*]|extern[ \t]+crate[ \t]+\w)")]
	private static partial Regex RustUseRegex();

	/// <summary>
	/// Matches the text of a line that ends a condition, a loop or a case of the shell, followed by a separator or
	/// a closing bracket at most.
	/// </summary>
	[GeneratedRegex(@"^(?:fi|done|esac)[;)]*$")]
	private static partial Regex ShellEndRegex();

	/// <summary>
	/// Matches a line that sources a script of the shell, but not a line that runs a script by its path.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*(?:source|\.)[ \t]+\S")]
	private static partial Regex ShellSourceRegex();

	/// <summary>
	/// Matches a line of a condition or a diagnostic of the compiler of Swift, but not a macro, which may have a body.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*#(?:if|elseif|else|endif|warning|error|sourceLocation)\b")]
	private static partial Regex SwiftDirectiveRegex();

	/// <summary>
	/// Matches a line that imports a module of Swift, also with attributes before it.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*(?:@\w+[ \t]+)*import[ \t]+\w")]
	private static partial Regex SwiftImportRegex();

	/// <summary>
	/// Matches a line that imports or includes a file of Typst.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*#(?:import|include)\b")]
	private static partial Regex TypstImportRegex();

	/// <summary>
	/// Matches the text of a line that ends a block of Visual Basic: an end statement, the next of a loop or a loop
	/// without a condition, followed by closing brackets at most.
	/// </summary>
	[GeneratedRegex(@"^(?:End[ \t]+[a-z]+|Next(?:[ \t]+\w+(?:[ \t]*,[ \t]*\w+)*)?|Loop)[)\]},]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex VisualBasicEndRegex();

	/// <summary>
	/// Matches a line that imports a namespace of Visual Basic.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*imports[ \t]+\S", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex VisualBasicImportsRegex();
	#endregion
}
