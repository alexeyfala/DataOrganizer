using AwesomeAssertions;
using DataOrganizer.Helpers.Text;
using System.Text.RegularExpressions;

namespace DataOrganizer.UnitTests.Helpers.Text;

[TestFixture(Description = $@"Tests of ""{nameof(SyntaxLinePatterns)}"" type")]
internal class SyntaxLinePatternsTests
{
	#region Methods
	/// <summary>
	/// <see cref="SyntaxLinePatterns.FindDirective" />: a line that starts with a number sign holds a directive or an
	/// attribute in a language of a preprocessor or of attributes.
	/// </summary>
	[TestCase("c", "#ifdef _WIN32")]
	[TestCase("cpp", "  #pragma once")]
	[TestCase("csharp", "#if DEBUG")]
	[TestCase("fsharp", "#nowarn \"40\"")]
	[TestCase("php", "#[Attribute]")]
	[TestCase("rust", "#[cfg(test)]")]
	[TestCase("swift", "#if canImport(UIKit)")]
	[TestCase("vb", "#If DEBUG Then")]
	public void FindDirective_Matches_A_Line_Of_A_Number_Sign(string language, string line)
	{
		// Act
		bool? isMatch = SyntaxLinePatterns
			.FindDirective(language)?
			.IsMatch(line);

		// Assert
		isMatch
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SyntaxLinePatterns.FindDirective" />: a language where a number sign starts code or a comment has no
	/// directives.
	/// </summary>
	[TestCase("css")]
	[TestCase("jade")]
	[TestCase("python")]
	[TestCase("typst")]
	public void FindDirective_Returns_Null_Where_A_Number_Sign_Starts_Code_Or_A_Comment(string language)
	{
		// Act
		Regex? pattern = SyntaxLinePatterns.FindDirective(language);

		// Assert
		pattern
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SyntaxLinePatterns.FindDocComment" />: a language whose documentation is no XML, or that has none, gets
	/// no token.
	/// </summary>
	[TestCase("python")]
	[TestCase("rust")]
	public void FindDocComment_Returns_Null_Without_Doc_Comments_Of_XML(string language)
	{
		// Act
		string? token = SyntaxLinePatterns.FindDocComment(language);

		// Assert
		token
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SyntaxLinePatterns.FindDocComment" />: a language with documentation comments of XML gets their token.
	/// </summary>
	[TestCase("csharp", "///")]
	[TestCase("fsharp", "///")]
	[TestCase("vb", "'''")]
	public void FindDocComment_Returns_The_Token_Of_The_Language(string language, string expected)
	{
		// Act
		string? token = SyntaxLinePatterns.FindDocComment(language);

		// Assert
		token
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="SyntaxLinePatterns.FindImport" />: a line that only looks like an import, such as a statement, a call or
	/// a variable, starts none.
	/// </summary>
	[TestCase("csharp", "using (var stream = Open())")]
	[TestCase("csharp", "using var stream = Open();")]
	[TestCase("csharp", "using FileStream stream = Open();")]
	[TestCase("javascript", "import('./a.js').then(run);")]
	[TestCase("typescript", "import (x)")]
	[TestCase("typescript", "import.meta.url")]
	[TestCase("less", "@use: 1px;")]
	[TestCase("makefile", "\tinclude a.mk")]
	[TestCase("python", "importlib.reload(a)")]
	[TestCase("r", "requireNamespace(\"a\")")]
	[TestCase("razor", "@using (Html.BeginForm())")]
	[TestCase("ruby", "require File.join(dir, 'a')")]
	[TestCase("shellscript", "./run.sh")]
	public void FindImport_Leaves_Out_The_Lines_That_Only_Look_Like_Imports(string language, string line)
	{
		// Act
		bool? isMatch = SyntaxLinePatterns
			.FindImport(language)?
			.IsMatch(line);

		// Assert
		isMatch
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SyntaxLinePatterns.FindImport" />: the import statements of a language are found by their first line.
	/// </summary>
	[TestCase("c", "#include <stdio.h>")]
	[TestCase("coffeescript", "import a from 'a'")]
	[TestCase("cpp", "# include \"a.h\"")]
	[TestCase("csharp", "using System.Linq;")]
	[TestCase("csharp", "using static System.Math;")]
	[TestCase("csharp", "using Alias = System.IO;")]
	[TestCase("csharp", "global using System;")]
	[TestCase("csharp", "extern alias Old;")]
	[TestCase("css", "@import url(\"a.css\");")]
	[TestCase("cuda-cpp", "#include <cuda.h>")]
	[TestCase("dart", "import 'package:a/a.dart';")]
	[TestCase("dart", "part of 'a.dart';")]
	[TestCase("fsharp", "open System")]
	[TestCase("go", "import \"fmt\"")]
	[TestCase("go", "import (")]
	[TestCase("groovy", "import groovy.json.JsonSlurper")]
	[TestCase("hlsl", "#include \"common.hlsl\"")]
	[TestCase("jade", "include mixins.pug")]
	[TestCase("java", "import java.util.List;")]
	[TestCase("java", "import static java.lang.Math.*;")]
	[TestCase("javascript", "import { a } from './a.js';")]
	[TestCase("javascriptreact", "import React from 'react';")]
	[TestCase("julia", "using LinearAlgebra")]
	[TestCase("latex", "\\usepackage[utf8]{inputenc}")]
	[TestCase("less", "@import (reference) 'a';")]
	[TestCase("lua", "local json = require(\"json\")")]
	[TestCase("makefile", "-include deps.mk")]
	[TestCase("objective-c", "#import <Foundation/Foundation.h>")]
	[TestCase("objective-cpp", "@import UIKit;")]
	[TestCase("perl", "use strict;")]
	[TestCase("perl6", "use Test;")]
	[TestCase("php", "use App\\Models\\User;")]
	[TestCase("php", "require_once 'a.php';")]
	[TestCase("powershell", "using namespace System.IO")]
	[TestCase("powershell", "Import-Module Az")]
	[TestCase("python", "import os")]
	[TestCase("python", "from . import views")]
	[TestCase("r", "library(dplyr)")]
	[TestCase("razor", "@using System.Linq")]
	[TestCase("ruby", "require_relative 'a'")]
	[TestCase("rust", "use std::io;")]
	[TestCase("rust", "pub(crate) use a::b;")]
	[TestCase("rust", "extern crate serde;")]
	[TestCase("scss", "@use 'sass:math';")]
	[TestCase("shaderlab", "#include \"UnityCG.cginc\"")]
	[TestCase("shellscript", "source ~/.bashrc")]
	[TestCase("shellscript", ". ./lib.sh")]
	[TestCase("swift", "@testable import App")]
	[TestCase("tex", "\\RequirePackage{a}")]
	[TestCase("typescript", "import type { A } from './a';")]
	[TestCase("typescriptreact", "import * as React from 'react';")]
	[TestCase("typst", "#import \"a.typ\": b")]
	[TestCase("vb", "Imports System.Text")]
	public void FindImport_Matches_The_Imports_Of_A_Language(string language, string line)
	{
		// Act
		bool? isMatch = SyntaxLinePatterns
			.FindImport(language)?
			.IsMatch(line);

		// Assert
		isMatch
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SyntaxLinePatterns.FindImport" />: a language whose imports fold in another way, or that has none, gets
	/// no pattern.
	/// </summary>
	[TestCase("bat")]
	[TestCase("clojure")]
	[TestCase("json")]
	[TestCase("pascal")]
	[TestCase("typst-code")]
	[TestCase("yaml")]
	public void FindImport_Returns_Null_Without_A_Run_Of_Imports(string language)
	{
		// Act
		Regex? pattern = SyntaxLinePatterns.FindImport(language);

		// Assert
		pattern
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SyntaxLinePatterns.FindLineComment" />: a word that only starts like a remark of Batch is none.
	/// </summary>
	[TestCase("remark")]
	[TestCase("echo REM")]
	public void FindLineComment_Leaves_Out_A_Word_That_Starts_Like_A_Remark_Of_Batch(string text)
	{
		// Act
		bool? isMatch = SyntaxLinePatterns
			.FindLineComment("bat", "@REM")?
			.IsMatch(text);

		// Assert
		isMatch
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SyntaxLinePatterns.FindLineComment" />: a remark of Batch starts with the word in any case, with or
	/// without the sign that hides it, or with a label that nothing jumps to.
	/// </summary>
	[TestCase("REM note")]
	[TestCase("rem note")]
	[TestCase("@REM note")]
	[TestCase("REM")]
	[TestCase(":: note")]
	public void FindLineComment_Matches_A_Remark_Of_Batch(string text)
	{
		// Act
		bool? isMatch = SyntaxLinePatterns
			.FindLineComment("bat", "@REM")?
			.IsMatch(text);

		// Assert
		isMatch
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SyntaxLinePatterns.FindLineComment" />: the token is matched at the start of a text only.
	/// </summary>
	[TestCase("// note", true)]
	[TestCase("a // note", false)]
	public void FindLineComment_Matches_The_Token_At_The_Start_Of_A_Text(string text, bool expected)
	{
		// Act
		bool? isMatch = SyntaxLinePatterns
			.FindLineComment("csharp", "//")?
			.IsMatch(text);

		// Assert
		isMatch
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="SyntaxLinePatterns.FindLineComment" />: a language without a token, or with a token of blanks, gets no
	/// pattern.
	/// </summary>
	[TestCase(null)]
	[TestCase("")]
	[TestCase(" ")]
	public void FindLineComment_Returns_Null_Without_A_Token(string? token)
	{
		// Act
		Regex? pattern = SyntaxLinePatterns.FindLineComment("xsl", token);

		// Assert
		pattern
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SyntaxLinePatterns.FindLineComment" />: the characters of a token are taken as they are, even those that
	/// mean something in a pattern.
	/// </summary>
	[TestCase("(* note", true)]
	[TestCase("( note", false)]
	public void FindLineComment_Takes_The_Token_As_It_Is(string text, bool expected)
	{
		// Act
		bool? isMatch = SyntaxLinePatterns
			.FindLineComment("pascal", "(*")?
			.IsMatch(text);

		// Assert
		isMatch
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="SyntaxLinePatterns.FindPreprocessor" />: a directive that may span lines, such as a definition of the C
	/// family or a macro of Swift with a body, does not match.
	/// </summary>
	[TestCase("c", "#define MAX(a, b) \\")]
	[TestCase("objective-c", "#define ANSWER 42")]
	[TestCase("swift", "#Preview {")]
	public void FindPreprocessor_Leaves_Out_A_Directive_That_May_Span_Lines(string language, string line)
	{
		// Act
		bool? isMatch = SyntaxLinePatterns
			.FindPreprocessor(language)?
			.IsMatch(line);

		// Assert
		isMatch
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SyntaxLinePatterns.FindPreprocessor" />: a directive of one line matches, indented or not.
	/// </summary>
	[TestCase("c", "#ifdef _WIN32")]
	[TestCase("cpp", "#pragma region Queue")]
	[TestCase("cpp", "  # endif")]
	[TestCase("csharp", "#if DEBUG")]
	[TestCase("csharp", "    #nullable enable")]
	[TestCase("fsharp", "#if DEBUG")]
	[TestCase("hlsl", "#pragma vertex vert")]
	[TestCase("objective-c", "#elif TARGET_OS_IOS")]
	[TestCase("swift", "#if os(iOS)")]
	[TestCase("swift", "#elseif DEBUG")]
	[TestCase("vb", "#If DEBUG Then")]
	[TestCase("vb", "#End Region")]
	public void FindPreprocessor_Matches_A_Directive_Of_One_Line(string language, string line)
	{
		// Act
		bool? isMatch = SyntaxLinePatterns
			.FindPreprocessor(language)?
			.IsMatch(line);

		// Assert
		isMatch
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SyntaxLinePatterns.FindPreprocessor" />: a language where a number sign starts an attribute, which may
	/// span lines, or a comment has no directives of one line.
	/// </summary>
	[TestCase("php")]
	[TestCase("python")]
	[TestCase("rust")]
	public void FindPreprocessor_Returns_Null_Where_A_Number_Sign_Starts_An_Attribute_Or_A_Comment(string language)
	{
		// Act
		Regex? pattern = SyntaxLinePatterns.FindPreprocessor(language);

		// Assert
		pattern
			.Should()
			.BeNull();
	}
	#endregion
}
