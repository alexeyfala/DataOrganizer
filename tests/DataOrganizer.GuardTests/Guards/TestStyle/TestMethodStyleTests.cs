using AwesomeAssertions;
using DataOrganizer.GuardTests.TestHelpers;
using DataOrganizer.GuardTests.TestHelpers.Dto;
using DataOrganizer.TestSupport.Guards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.GuardTests.Guards.TestStyle;

[Guard]
[TestFixture(Description = "Guards the methods of the test classes against checks that the test style rules out")]
internal sealed partial class TestMethodStyleTests
{
	#region Data
	/// <summary>
	/// The methods of the test classes, read once.
	/// </summary>
	private static readonly Lazy<TestMethod[]> TestMethods = new(static () =>
	[
		.. RepositoryFiles
			.EnumerateTestCode()
			.SelectMany(static x => TestMethodReader.Read(x.FilePath, x.Text))
	]);
	#endregion

	#region Methods
	/// <summary>
	/// No assertion ends a null-conditional chain, which skips the assertion when the value is <c>null</c>.
	/// </summary>
	[Test]
	public void Assertions_Do_Not_End_A_Null_Conditional_Chain()
	{
		// Act
		string[] skippable =
		[
			.. TestMethods.Value
				.Where(static x => NullConditionalAssertionRegex().IsMatch(x.Code))
				.Select(Describe)
		];

		// Assert
		TestMethods.Value
			.Should()
			.NotBeEmpty();

		skippable
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// No check stands inside a loop, where it stops at the first miss without naming the others.
	/// </summary>
	[Test]
	public void Checks_Stand_Outside_Loops()
	{
		// Act
		string[] looped =
		[
			.. TestMethods.Value
				.Where(static x => FindLoopBodies(x.Code).Any(CheckRegex().IsMatch))
				.Select(Describe)
		];

		// Assert
		TestMethods.Value
			.Should()
			.NotBeEmpty();

		looped
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// A method that is no test neither returns a container nor checks anything: each test shows its whole arrangement and
	/// writes its own checks.
	/// </summary>
	[Test]
	public void Helpers_Neither_Set_Up_Containers_Nor_Check()
	{
		// Act
		string[] helpers =
		[
			.. TestMethods.Value
				.Where(static x => !x.IsTest && (AutoMockRegex().IsMatch(x.ReturnType) || CheckRegex().IsMatch(x.Code)))
				.Select(Describe)
		];

		// Assert
		TestMethods.Value
			.Should()
			.NotBeEmpty();

		helpers
			.Should()
			.HaveCount(0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Matches the type of a test container.
	/// </summary>
	[GeneratedRegex(@"\bAutoMock\b")]
	private static partial Regex AutoMockRegex();

	/// <summary>
	/// Matches an assertion or a received-check.
	/// </summary>
	[GeneratedRegex(@"\.\s*Should\s*\(\s*\)|\.\s*(?:Received|DidNotReceive)(?:WithAnyArgs)?\s*\(|\bReceived\s*\.\s*InOrder\b")]
	private static partial Regex CheckRegex();

	/// <summary>
	/// Describes a method by its file and its name.
	/// </summary>
	private static string Describe(TestMethod method) => $"{method.FilePath} {method.Name}";

	/// <summary>
	/// Finds the bodies of the <c>for</c> and <c>foreach</c> loops of a method: a block or a single statement each.
	/// </summary>
	private static IEnumerable<string> FindLoopBodies(string code)
	{
		foreach (Match loop in LoopRegex().Matches(code))
		{
			int start = CodeText.FindClosing(code, loop.Groups["open"].Index) + 1;

			while (start < code.Length && char.IsWhiteSpace(code[start]))
			{
				start++;
			}

			int end = start < code.Length && code[start] == '{'
				? CodeText.FindClosing(code, start)
				: CodeText.FindStatementEnd(code, start);

			yield return code[start..Math.Min(end + 1, code.Length)];
		}
	}

	/// <summary>
	/// Matches the header of a <c>for</c> or <c>foreach</c> loop up to the opening parenthesis of its clause.
	/// </summary>
	[GeneratedRegex(@"\b(?:foreach|for)\s*(?<open>\()")]
	private static partial Regex LoopRegex();

	/// <summary>
	/// Matches a null-conditional access whose chain reaches an assertion.
	/// </summary>
	[GeneratedRegex(@"\?\s*[.\[](?:[^;{}()]|\([^()]*\))*?\.Should\(\)")]
	private static partial Regex NullConditionalAssertionRegex();
	#endregion
}
