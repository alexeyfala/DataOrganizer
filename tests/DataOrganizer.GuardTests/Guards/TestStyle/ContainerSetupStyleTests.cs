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
[TestFixture(Description = "Guards the containers of the tests against setups that the test style rules out")]
internal sealed partial class ContainerSetupStyleTests
{
	#region Data
	/// <summary>
	/// The containers the tests set up, read once.
	/// </summary>
	private static readonly Lazy<ContainerSetup[]> Setups = new(static () =>
	[
		.. RepositoryFiles
			.EnumerateTestCode()
			.SelectMany(static x => ContainerSetupReader.Read(x.FilePath, x.Text))
	]);
	#endregion

	#region Methods
	/// <summary>
	/// After its first registration a setup holds only registrations: its doubles are declared and configured above them.
	/// </summary>
	[Test]
	public void Setups_End_With_Their_Registrations()
	{
		// Act
		string[] misplaced =
		[
			.. Setups.Value.SelectMany(static setup => FindAfterRegistrations(setup)
				.Select(statement => Describe(setup, statement.Split('\n')[0].Trim())))
		];

		// Assert
		Setups.Value
			.Should()
			.NotBeEmpty();

		misplaced
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// A double declared outside a setup and used in it is read after the setup: only what the assertions read stays
	/// outside.
	/// </summary>
	[Test]
	public void Tests_Read_The_Doubles_They_Declare_Outside_A_Setup()
	{
		// Act
		string[] unread =
		[
			.. Setups.Value.SelectMany(static setup => FindUnreadDoubles(setup)
				.Select(name => Describe(setup, name)))
		];

		// Assert
		Setups.Value
			.Should()
			.NotBeEmpty();

		unread
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// An instance built with <c>new</c> right in its registration is registered as its type, which the container builds.
	/// </summary>
	[Test]
	public void Tests_Register_Unread_Instances_As_Types()
	{
		// Act
		string[] instances =
		[
			.. Setups.Value.SelectMany(static setup => setup
				.Registrations
				.Where(static x => NewInstanceRegex().IsMatch(x.Text))
				.Select(x => Describe(setup, x.Text)))
		];

		// Assert
		Setups.Value
			.Should()
			.NotBeEmpty();

		instances
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// A mock file system the container builds is one instance for all its consumers, so a file one writes the next finds.
	/// </summary>
	[Test]
	public void Tests_Share_One_Mock_File_System_In_A_Container()
	{
		// Act
		string[] separate =
		[
			.. Setups.Value.SelectMany(static setup => setup
				.Registrations
				.Where(static x => x.Implementation == "MockFileSystem" && !x.IsSingleInstance)
				.Select(x => Describe(setup, x.Text)))
		];

		// Assert
		Setups.Value
			.Should()
			.NotBeEmpty();

		separate
			.Should()
			.HaveCount(0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Describes a finding in a setup by its file, its test method and its detail.
	/// </summary>
	private static string Describe(ContainerSetup setup, string detail) => $"{setup.FilePath} {setup.Method}: {detail}";

	/// <summary>
	/// Matches the declaration of a double and its name.
	/// </summary>
	[GeneratedRegex(@"^\s*[\w<>,\[\]?. ]+?\s+(?<name>\w+)\s*=\s*Substitute\s*\.\s*(?:For|ForPartsOf)\b", RegexOptions.Multiline)]
	private static partial Regex DoubleDeclarationRegex();

	/// <summary>
	/// Matches an <c>else</c> that opens the rest of a statement.
	/// </summary>
	[GeneratedRegex(@"^else\b")]
	private static partial Regex ElseRegex();

	/// <summary>
	/// Finds the statements of a setup after its first registration that are no registrations.
	/// </summary>
	private static IEnumerable<string> FindAfterRegistrations(ContainerSetup setup)
	{
		Regex registration = new($@"^{Regex.Escape(setup.Builder)}\s*\.");

		int first = Array.FindIndex(setup.Statements, x => IsRegistration(x, registration));

		return first < 0 ? [] : setup.Statements.Skip(first + 1).Where(x => !IsRegistration(x, registration));
	}

	/// <summary>
	/// Finds the doubles declared before a setup and used in it that the test does not read after it.
	/// </summary>
	private static IEnumerable<string> FindUnreadDoubles(ContainerSetup setup)
	{
		string statements = string.Join('\n', setup.Statements);

		foreach (Match declaration in DoubleDeclarationRegex().Matches(setup.CodeBefore))
		{
			string name = declaration
				.Groups["name"]
				.Value;

			Regex use = new($@"\b{name}\b");

			if (use.IsMatch(statements) && !use.IsMatch(setup.CodeAfter))
			{
				yield return name;
			}
		}
	}

	/// <summary>
	/// Matches an <c>if</c> statement.
	/// </summary>
	[GeneratedRegex(@"^if\s*\(")]
	private static partial Regex IfRegex();

	/// <summary>
	/// Tells whether an <c>if</c> statement only registers: each of its branches does, the <c>else</c> one included.
	/// </summary>
	private static bool IsIfOfRegistrations(string statement, Regex registration)
	{
		string rest = statement[(CodeText.FindClosing(statement, statement.IndexOf('(')) + 1)..].TrimStart();

		int end = rest.StartsWith('{') ? CodeText.FindClosing(rest, 0) : CodeText.FindStatementEnd(rest, 0);

		string alternative = rest[(end + 1)..].Trim();

		return IsRegistration(rest[..(end + 1)], registration)
			&& (alternative.Length == 0
				|| (ElseRegex().Match(alternative) is { Success: true } other
					&& IsRegistration(alternative[other.Length..].TrimStart(), registration)));
	}

	/// <summary>
	/// Tells whether a statement only registers: a call on the builder, or a block or an <c>if</c> whose statements
	/// only register.
	/// </summary>
	private static bool IsRegistration(string statement, Regex registration)
	{
		if (registration.IsMatch(statement))
		{
			return true;
		}

		if (statement.StartsWith('{'))
		{
			return CodeText
				.SplitStatements(statement[1..CodeText.FindClosing(statement, 0)])
				.All(x => IsRegistration(x, registration));
		}

		return IfRegex().IsMatch(statement) && IsIfOfRegistrations(statement, registration);
	}

	/// <summary>
	/// Matches the registration of an instance built with <c>new</c> and no arguments.
	/// </summary>
	[GeneratedRegex(@"^RegisterInstance(?:<[^()]*>)?\(new\s+[\w.]+(?:<[^()]*>)?\s*\(\s*\)\)$")]
	private static partial Regex NewInstanceRegex();
	#endregion
}
