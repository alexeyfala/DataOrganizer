using DataOrganizer.GuardTests.TestHelpers.Dto;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DataOrganizer.GuardTests.TestHelpers;

/// <summary>
/// Reads from the source of a test file the methods of its classes.
/// </summary>
internal static partial class TestMethodReader
{
	#region Methods
	/// <summary>
	/// Reads the methods of the classes of a test file in the order of the file.
	/// </summary>
	public static IEnumerable<TestMethod> Read(string filePath, string text)
	{
		string code = CodeText.WithoutCommentsAndLiterals(text);

		int previousEnd = 0;

		foreach (Match header in HeaderRegex().Matches(code))
		{
			if (header.Index < previousEnd)
			{
				continue;
			}

			int end = FindEnd(code, CodeText.FindClosing(code, header.Groups["open"].Index) + 1);

			yield return new TestMethod
			{
				Code = code[header.Index..end],
				FilePath = filePath,
				IsTest = TestAttributeRegex().IsMatch(code[previousEnd..header.Index]),
				Name = header.Groups["name"].Value,
				ReturnType = ModifierRegex().Replace(header.Groups["head"].Value, string.Empty).Trim()
			};

			previousEnd = end;
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Finds the end of a method from the end of its parameter list: the end of its block, of its expression body or of
	/// a declaration without a body.
	/// </summary>
	private static int FindEnd(string code, int start)
	{
		for (int i = start; i < code.Length; i++)
		{
			switch (code[i])
			{
				case '{':
					return CodeText.FindClosing(code, i) + 1;
				case ';':
					return i + 1;
				case '=' when i + 1 < code.Length && code[i + 1] == '>':
					return CodeText.FindStatementEnd(code, i + 2) + 1;
			}
		}

		return code.Length;
	}

	/// <summary>
	/// Matches the header of a method of a class up to its parameter list, with its modifiers and return type, its name
	/// and the opening parenthesis of its parameters.
	/// </summary>
	[GeneratedRegex(@"^\t(?<head>(?:public|private|internal|protected)\b[^\r\n=]*?)\b(?<name>\w+)\s*(?:<[^\r\n]*?>)?\s*(?<open>\()", RegexOptions.Multiline)]
	private static partial Regex HeaderRegex();

	/// <summary>
	/// Matches a modifier of a method.
	/// </summary>
	[GeneratedRegex(@"\b(?:abstract|async|extern|internal|new|override|partial|private|protected|public|sealed|static|unsafe|virtual)\b")]
	private static partial Regex ModifierRegex();

	/// <summary>
	/// Matches an attribute that makes a method a test.
	/// </summary>
	[GeneratedRegex(@"\[\s*(?:AvaloniaTest|Test|TestCase|TestCaseSource|Theory)\b")]
	private static partial Regex TestAttributeRegex();
	#endregion
}
