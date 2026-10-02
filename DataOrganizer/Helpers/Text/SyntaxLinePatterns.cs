using System.Text.RegularExpressions;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Patterns and tokens of the lines of a language that folding tells apart: imports, directives, comments and the ends
/// of blocks.
/// </summary>
internal static partial class SyntaxLinePatterns
{
	#region Data
	/// <summary>
	/// Language of Batch, whose remarks start with a word as well as with a mark.
	/// </summary>
	private const string BatchLanguage = "bat";
	#endregion

	#region Methods
	/// <summary>
	/// Returns the pattern of a line of a language that holds a directive to the compiler or an attribute; <c>null</c> when
	/// such lines start code or comments in the language.
	/// </summary>
	public static Regex? FindDirective(string language) => language switch
	{
		// The preprocessor of the C family, C#, F#, Swift and Visual Basic, the attributes of Rust and PHP.
		"c" or "cpp" or "cuda-cpp" or "hlsl" or "objective-c" or "objective-cpp" or "shaderlab" => DirectiveRegex(),
		"csharp" or "fsharp" or "php" or "rust" or "swift" or "vb" => DirectiveRegex(),
		_ => null
	};

	/// <summary>
	/// Returns the token that opens a line of a documentation comment of XML in a language; <c>null</c> when the language
	/// has no such comments.
	/// </summary>
	public static string? FindDocComment(string language) => language switch
	{
		"csharp" or "fsharp" => "///",
		"vb" => "'''",
		_ => null
	};

	/// <summary>
	/// Returns the pattern of the text of a line of a language that ends the block above it with a word or a tag alone;
	/// <c>null</c> when the blocks of the language end with brackets, or start with a word on a line of its own.
	/// </summary>
	public static Regex? FindEndLine(string language) => language switch
	{
		"html" or "razor" => HtmlEndTagRegex(),
		"julia" or "lua" or "ruby" => EndWordRegex(),
		"latex" or "tex" => LatexEndRegex(),
		"makefile" => MakefileEndRegex(),
		"shellscript" => ShellEndRegex(),
		"vb" => VisualBasicEndRegex(),
		_ => null
	};

	/// <summary>
	/// Returns the pattern of a line of a language that starts an import statement; <c>null</c> when the imports of the
	/// language do not fold as a run.
	/// </summary>
	public static Regex? FindImport(string language) => language switch
	{
		"c" or "cpp" or "cuda-cpp" or "hlsl" or "shaderlab" => CIncludeRegex(),
		"coffeescript" or "javascript" or "javascriptreact" or "typescript" or "typescriptreact" => JavaScriptImportRegex(),
		"csharp" => CSharpUsingRegex(),
		"css" or "less" or "scss" => CssImportRegex(),
		"dart" => DartImportRegex(),
		"fsharp" => FSharpOpenRegex(),
		"go" => GoImportRegex(),
		"groovy" or "java" => JavaImportRegex(),
		"jade" => PugIncludeRegex(),
		"julia" => JuliaImportRegex(),
		"latex" or "tex" => LatexPackageRegex(),
		"lua" => LuaRequireRegex(),
		"makefile" => MakefileIncludeRegex(),
		"objective-c" or "objective-cpp" => ObjectiveCImportRegex(),
		"perl" or "perl6" => PerlUseRegex(),
		"php" => PhpUseRegex(),
		"powershell" => PowerShellUsingRegex(),
		"python" => PythonImportRegex(),
		"r" => RLibraryRegex(),
		"razor" => RazorUsingRegex(),
		"ruby" => RubyRequireRegex(),
		"rust" => RustUseRegex(),
		"shellscript" => ShellSourceRegex(),
		"swift" => SwiftImportRegex(),
		"typst" => TypstImportRegex(),
		"vb" => VisualBasicImportsRegex(),
		_ => null
	};

	/// <summary>
	/// Returns the pattern of the token that opens a line comment of a language, matched at the start of a text;
	/// <c>null</c> when the language has no token.
	/// </summary>
	public static Regex? FindLineComment(string language, string? token)
	{
		// A remark of Batch is a word in any case or a label that nothing jumps to, beside the token of its settings.
		if (language == BatchLanguage)
		{
			return BatchRemarkRegex();
		}

		// A token of blanks, as one language has, would find a comment in every text.
		if (string.IsNullOrWhiteSpace(token))
		{
			return null;
		}

		// Compiled, as a pass may try it on each line.
		return new Regex("^" + Regex.Escape(token), RegexOptions.Compiled);
	}

	/// <summary>
	/// Returns the pattern of a line of a language that holds a directive standing apart from the indentation of the code,
	/// such as a condition of the preprocessor; <c>null</c> when the lines with a number sign belong to the code.
	/// </summary>
	public static Regex? FindPreprocessor(string language) => language switch
	{
		// A directive of C#, F# and Visual Basic takes one line, so every line with a number sign holds one.
		"csharp" or "fsharp" or "vb" => DirectiveRegex(),
		"c" or "cpp" or "cuda-cpp" or "hlsl" or "objective-c" or "objective-cpp" or "shaderlab" => CPreprocessorRegex(),
		"swift" => SwiftDirectiveRegex(),
		_ => null
	};
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
