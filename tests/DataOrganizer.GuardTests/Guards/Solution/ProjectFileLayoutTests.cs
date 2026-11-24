using AwesomeAssertions;
using DataOrganizer.TestSupport.Guards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace DataOrganizer.GuardTests.Guards.Solution;

[Guard]
[TestFixture(Description = "Guards the layout of the MSBuild files of the repository")]
internal sealed class ProjectFileLayoutTests
{
	#region Data
	/// <summary>
	/// Patterns of the MSBuild files.
	/// </summary>
	private static readonly string[] BuildFilePatterns = ["*.csproj", "*.props", "*.targets", "*.wixproj"];

	/// <summary>
	/// Kinds of the items that open a C# project, in their order; the items of other kinds follow them.
	/// </summary>
	private static readonly string[] LeadingKinds = ["PackageReference", "ProjectReference", "InternalsVisibleTo"];
	#endregion

	#region Methods
	/// <summary>
	/// In every item group, the items of one kind and one operation follow their values alphabetically, regardless of
	/// case.
	/// </summary>
	[Test]
	public void Items_Of_One_Kind_Follow_Their_Values_Alphabetically()
	{
		// Act
		string[] violations =
		[
			.. EnumerateProjects(BuildFilePatterns)
				.SelectMany(static file => file.Project
					.Descendants("ItemGroup")
					.SelectMany(FindUnsortedItems)
					.Select(x => $"{file.FilePath}: {x}"))
		];

		// Assert
		violations
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// Every C# project opens with its package references, project references and friend assemblies, in this order.
	/// </summary>
	[Test]
	public void Projects_Open_With_References_In_Order()
	{
		// Act
		string[] violations =
		[
			.. EnumerateProjects("*.csproj")
				.Select(static file => (file.FilePath, Ranks: ReadRanks(file.Project)))
				.Where(static x => !x.Ranks.SequenceEqual(x.Ranks.Order()))
				.Select(static x => x.FilePath)
		];

		// Assert
		violations
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// The elements at the top of every MSBuild file are set apart by one blank line, with none after the opening tag or
	/// before the closing one; a comment and a region marker stay next to what they mark.
	/// </summary>
	[Test]
	public void Top_Elements_Are_Set_Apart_By_One_Blank_Line()
	{
		// Act
		string[] violations =
		[
			.. EnumerateProjects(BuildFilePatterns)
				.SelectMany(static file => FindWrongGaps(file.Project)
					.Select(x => $"{file.FilePath}: {x}"))
		];

		// Assert
		violations
			.Should()
			.HaveCount(0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Parses the MSBuild files that match the patterns, whitespace and line numbers kept, each paired with its
	/// repository-relative path.
	/// </summary>
	private static IEnumerable<(string FilePath, XElement Project)> EnumerateProjects(params string[] patterns)
	{
		return RepositoryFiles
			.EnumerateRepositoryFiles(patterns)
			.Select(static x => (x.FilePath, Project: XDocument.Parse(x.Text, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo).Root!));
	}

	/// <summary>
	/// Describes each item of a group whose value sorts before the value of the previous item of the same kind and
	/// operation.
	/// </summary>
	private static IEnumerable<string> FindUnsortedItems(XElement group)
	{
		return group
			.Elements()
			.Select(static x => x.Attributes().Single(static y => y.Name.LocalName is "Include" or "Remove" or "Update"))
			.GroupBy(static x => $"{x.Parent!.Name} {x.Name}")
			.SelectMany(static x => x.Zip(x.Skip(1)))
			.Where(static x => StringComparer.OrdinalIgnoreCase.Compare(x.First.Value, x.Second.Value) > 0)
			.Select(static x => $"{x.Second.Parent!.Name} {x.Second.Name}=\"{x.Second.Value}\" after \"{x.First.Value}\"");
	}

	/// <summary>
	/// Describes each gap between the tags of a project and its top elements and comments whose number of blank lines
	/// differs from the expected one.
	/// </summary>
	private static IEnumerable<string> FindWrongGaps(XElement project)
	{
		XNode[] nodes = [.. project.Nodes().Where(static x => x is not XText)];

		for (int i = 0; i <= nodes.Length; i++)
		{
			XNode? previous = i > 0 ? nodes[i - 1] : null;

			XNode? next = i < nodes.Length ? nodes[i] : null;

			bool joined = previous is null
				|| next is null
				|| IsRegionEnd(next)
				|| (previous is XComment && !IsRegionEnd(previous));

			int expected = joined ? 0 : 1;

			int? actual = (previous is null ? project.FirstNode : previous.NextNode) is XText gap
				? gap.Value.Count(static x => x == '\n') - 1
				: null;

			if (actual == expected)
			{
				continue;
			}

			string place = next is null ? $"before </{project.Name}>" : $"before line {((IXmlLineInfo)next).LineNumber}";

			yield return actual is null ? $"no line break {place}" : $"{actual} blank lines {place}, {expected} expected";
		}
	}

	/// <summary>
	/// Tells whether a node is the comment that closes a region.
	/// </summary>
	private static bool IsRegionEnd(XNode node)
	{
		return node is XComment comment && comment.Value.Trim() == "#endregion";
	}

	/// <summary>
	/// Reads the rank of each item of a project in the order of the file: the index of its kind among the leading kinds,
	/// or their count for any other kind.
	/// </summary>
	private static int[] ReadRanks(XElement project)
	{
		return
		[
			.. project
				.Elements("ItemGroup")
				.Elements()
				.Select(static x => Array.IndexOf(LeadingKinds, x.Name.LocalName))
				.Select(static x => x < 0 ? LeadingKinds.Length : x)
		];
	}
	#endregion
}
