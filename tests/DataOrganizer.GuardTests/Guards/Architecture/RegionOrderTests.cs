using AwesomeAssertions;
using DataOrganizer.GuardTests.TestHelpers;
using DataOrganizer.TestSupport.Guards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.GuardTests.Guards.Architecture;

[Guard]
[TestFixture(Description = "Guards the order of the regions that lay out the members of a type")]
internal sealed partial class RegionOrderTests
{
	#region Data
	/// <summary>
	/// Regions that lay out the members of a type, in the order a type holds them.
	/// </summary>
	private static readonly string[] LayoutOrder =
	[
		"Properties",
		"Styled Properties",
		"Direct Properties",
		"Partial",
		"Commands",
		"Auto-Generated Commands",
		"Data",
		"Constructors",
		"Event Handlers",
		"Methods",
		"Helpers",
		"Native",
		"Nested Types"
	];
	#endregion

	#region Methods
	/// <summary>
	/// The layout regions of every type of the code and of the tests follow <see cref="LayoutOrder" />; a nested type
	/// keeps an order of its own.
	/// </summary>
	[Test]
	public void Layout_Regions_Of_A_Type_Follow_One_Order()
	{
		// Act
		(string FilePath, string Text)[] files =
		[
			.. RepositoryFiles
				.EnumerateSourceCode()
				.Select(static x => (FilePath: $"src/{x.FilePath}", x.Text)),
			.. RepositoryFiles
				.EnumerateTestCode()
				.Select(static x => (FilePath: $"tests/{x.FilePath}", x.Text))
		];

		string[] misplaced = [.. files.SelectMany(static x => FindMisplaced(x.FilePath, x.Text))];

		// Assert
		files
			.Should()
			.NotBeEmpty();

		misplaced
			.Should()
			.HaveCount(0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Finds the opening brace of the body of a type from the end of its name, or <c>-1</c> for a type without a body.
	/// </summary>
	private static int FindBody(string code, int start)
	{
		for (int i = start; i < code.Length; i++)
		{
			switch (code[i])
			{
				case '(' or '[':
					i = CodeText.FindClosing(code, i);
					break;
				case '{':
					return i;
				case ';':
					return -1;
			}
		}

		return -1;
	}

	/// <summary>
	/// Finds the layout regions of a file that stand below a region the order puts after them, each as its file, its
	/// type and the two regions.
	/// </summary>
	private static IEnumerable<string> FindMisplaced(string filePath, string text)
	{
		string code = CodeText.WithoutCommentsAndLiterals(text);

		(int Open, int Close, string Name)[] types =
		[
			.. TypeRegex()
				.Matches(code)
				.Select(x => (Open: FindBody(code, x.Index + x.Length), Name: x.Groups["name"].Value))
				.Where(static x => x.Open >= 0)
				.Select(x => (x.Open, Close: CodeText.FindClosing(code, x.Open), x.Name))
		];

		IEnumerable<IGrouping<(int Open, int Close, string Name), Match>> regionsByType = RegionRegex()
			.Matches(code)
			.Where(static x => LayoutOrder.Contains(x.Groups["name"].Value))
			.Select(x => (Region: x, Owner: types.LastOrDefault(type => type.Open < x.Index && x.Index < type.Close)))
			.Where(static x => x.Owner.Name is not null)
			.GroupBy(static x => x.Owner, static x => x.Region);

		foreach (IGrouping<(int Open, int Close, string Name), Match> regions in regionsByType)
		{
			string previous = string.Empty;

			foreach (string name in regions.Select(static x => x.Groups["name"].Value))
			{
				if (previous.Length > 0 && Array.IndexOf(LayoutOrder, name) < Array.IndexOf(LayoutOrder, previous))
				{
					yield return $"{filePath} {regions.Key.Name}: {name} stands below {previous}";

					continue;
				}

				previous = name;
			}
		}
	}

	/// <summary>
	/// Matches a region with its name.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*#region[ \t]+(?<name>[^\r\n]*?)[ \t]*\r?$", RegexOptions.Multiline)]
	private static partial Regex RegionRegex();

	/// <summary>
	/// Matches the declaration of a type up to its name.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*(?:(?:public|internal|private|protected|static|sealed|abstract|partial|file|readonly|ref|unsafe|new)\s+)*(?:class|struct|interface|enum|record(?:\s+(?:class|struct))?)\s+(?<name>\w+)", RegexOptions.Multiline)]
	private static partial Regex TypeRegex();
	#endregion
}
