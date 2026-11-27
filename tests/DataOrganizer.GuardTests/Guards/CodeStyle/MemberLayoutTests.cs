using AwesomeAssertions;
using DataOrganizer.GuardTests.TestHelpers;
using DataOrganizer.TestSupport.Guards;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.GuardTests.Guards.CodeStyle;

[Guard]
[TestFixture(Description = "Guards the layout of the members of a type: their order and the blank lines around them")]
internal sealed class MemberLayoutTests
{
	#region Methods
	/// <summary>
	/// The members of every type stand, inside each region, in the order the Reorganize command of CodeMaid (Ctrl+M, Z)
	/// gives with its default settings: the guard repeats its algorithm, <see cref="CodeMaidMemberOrder" />.
	/// </summary>
	[Test]
	public void Members_Follow_The_Order_Of_CodeMaid()
	{
		// Act
		IReadOnlyList<SyntaxTree> trees = SourceSyntax.GetTrees();

		string[] misplaced =
		[
			.. trees
				.SelectMany(static x => x.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
				.Where(CodeMaidMemberOrder.SortsMembersOf)
				.SelectMany(FindMisplaced)
		];

		// Assert
		trees
			.Should()
			.NotBeEmpty();

		misplaced
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// Neighbouring members of a type and neighbouring values of an enum have a blank line between them, above the summary
	/// of the second; a region boundary between them keeps its own layout.
	/// </summary>
	[Test]
	public void Neighbours_Stand_Apart_By_A_Blank_Line()
	{
		// Act
		(SyntaxNode Previous, SyntaxNode Next)[] neighbours = [.. EnumerateNeighbours()];

		string[] touching =
		[
			.. neighbours
				.Where(IsTouching)
				.Select(static x => $"{Describe(x.Next)} touches {GetName(x.Previous)}")
		];

		// Assert
		neighbours
			.Should()
			.NotBeEmpty();

		touching
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// Neighbouring members of a type and neighbouring values of an enum have no more than one blank line in a row between
	/// them.
	/// </summary>
	[Test]
	public void Neighbours_Stand_No_More_Than_One_Blank_Line_Apart()
	{
		// Act
		(SyntaxNode Previous, SyntaxNode Next)[] neighbours = [.. EnumerateNeighbours()];

		string[] distant =
		[
			.. neighbours
				.Where(HasDoubleBlankLine)
				.Select(static x => $"{Describe(x.Next)} stands more than one blank line below {GetName(x.Previous)}")
		];

		// Assert
		neighbours
			.Should()
			.NotBeEmpty();

		distant
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// The summary of a member of a type or of a value of an enum stands right above it, with no blank line between.
	/// </summary>
	[Test]
	public void Summary_Stands_Right_Above_Its_Member()
	{
		// Act
		SyntaxNode[] members = [.. EnumerateMemberLists().SelectMany(static x => x)];

		string[] parted =
		[
			.. members
				.Where(IsPartedFromSummary)
				.Select(static x => $"{Describe(x)} stands apart from its summary")
		];

		// Assert
		members
			.Should()
			.NotBeEmpty();

		parted
			.Should()
			.HaveCount(0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Describes a member by its file, its line and its name.
	/// </summary>
	private static string Describe(SyntaxNode member) => $"{member.SyntaxTree.FilePath}:{GetLine(member, member.SpanStart) + 1} {GetName(member)}";

	/// <summary>
	/// Enumerates the members of every type and the values of every enum, one list per type.
	/// </summary>
	private static IEnumerable<IReadOnlyList<SyntaxNode>> EnumerateMemberLists()
	{
		IEnumerable<BaseTypeDeclarationSyntax> types = SourceSyntax
			.GetTrees()
			.SelectMany(static x => x.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>());

		foreach (BaseTypeDeclarationSyntax type in types)
		{
			yield return type switch
			{
				EnumDeclarationSyntax x => x.Members,
				TypeDeclarationSyntax x => x.Members,
				_ => []
			};
		}
	}

	/// <summary>
	/// Enumerates every pair of neighbouring members of a type and of neighbouring values of an enum.
	/// </summary>
	private static IEnumerable<(SyntaxNode Previous, SyntaxNode Next)> EnumerateNeighbours()
	{
		foreach (IReadOnlyList<SyntaxNode> members in EnumerateMemberLists())
		{
			for (int i = 1; i < members.Count; i++)
			{
				yield return (members[i - 1], members[i]);
			}
		}
	}

	/// <summary>
	/// Finds the members of a type that stand below a neighbour of their region that CodeMaid puts after them.
	/// </summary>
	private static IEnumerable<string> FindMisplaced(TypeDeclarationSyntax type)
	{
		Stack<int> regions = new();
		Dictionary<int, MemberDeclarationSyntax> lastByRegion = [];
		int opened = 0;

		foreach (MemberDeclarationSyntax member in type.Members)
		{
			foreach (SyntaxTrivia directive in member.GetLeadingTrivia().Where(static x => x.IsDirective))
			{
				if (directive.IsKind(SyntaxKind.RegionDirectiveTrivia))
				{
					regions.Push(++opened);
				}
				else if (directive.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
				{
					regions.TryPop(out _);
				}
			}

			int region = regions.TryPeek(out int innermost) ? innermost : 0;

			if (lastByRegion.TryGetValue(region, out MemberDeclarationSyntax? previous) && CodeMaidMemberOrder.Compare(previous, member) > 0)
			{
				yield return $"{Describe(member)} stands below {GetName(previous)}";
			}

			lastByRegion[region] = member;
		}
	}

	/// <summary>
	/// Finds the summary of a member among the trivia before it, or the default trivia when it has none.
	/// </summary>
	private static SyntaxTrivia FindSummary(SyntaxNode member)
	{
		return member
			.GetLeadingTrivia()
			.FirstOrDefault(static x => x.GetStructure() is DocumentationCommentTriviaSyntax);
	}

	/// <summary>
	/// Gets the line a member ends on, the comma after an enum value included.
	/// </summary>
	private static int GetEndLine(SyntaxNode member)
	{
		SyntaxToken last = member.GetLastToken();
		SyntaxToken separator = last.GetNextToken();

		bool endsWithComma = member is EnumMemberDeclarationSyntax && separator.IsKind(SyntaxKind.CommaToken);

		return GetLine(member, endsWithComma ? separator.SpanStart : last.SpanStart);
	}

	/// <summary>
	/// Gets the zero-based line of a position in the file of a member.
	/// </summary>
	private static int GetLine(SyntaxNode member, int position)
	{
		return member
			.SyntaxTree
			.GetText()
			.Lines
			.GetLineFromPosition(position)
			.LineNumber;
	}

	/// <summary>
	/// Gets the name of a member of a type or of a value of an enum.
	/// </summary>
	private static string GetName(SyntaxNode member)
	{
		return member switch
		{
			EnumMemberDeclarationSyntax x => x.Identifier.Text,
			MemberDeclarationSyntax x => CodeMaidMemberOrder.GetName(x),
			_ => string.Empty
		};
	}

	/// <summary>
	/// Gets the line a member starts on: the first line of its summary, else the line of its first token.
	/// </summary>
	private static int GetSummaryLine(SyntaxNode member)
	{
		SyntaxTrivia summary = FindSummary(member);

		return GetLine(member, summary.IsKind(SyntaxKind.None) ? member.SpanStart : summary.SpanStart);
	}

	/// <summary>
	/// Tells whether two blank lines in a row stand between two neighbours.
	/// </summary>
	private static bool HasDoubleBlankLine((SyntaxNode Previous, SyntaxNode Next) neighbours)
	{
		int first = GetEndLine(neighbours.Previous) + 1;
		int last = GetLine(neighbours.Next, neighbours.Next.SpanStart) - 1;

		string[] lines = [.. ReadLines(neighbours.Next, first, last)];

		for (int i = 1; i < lines.Length; i++)
		{
			if (string.IsNullOrWhiteSpace(lines[i - 1]) && string.IsNullOrWhiteSpace(lines[i]))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Tells whether a blank line stands between a member and its summary.
	/// </summary>
	private static bool IsPartedFromSummary(SyntaxNode member)
	{
		SyntaxTrivia summary = FindSummary(member);

		if (summary.IsKind(SyntaxKind.None))
		{
			return false;
		}

		return ReadLines(member, GetLine(member, summary.Span.End - 1) + 1, GetLine(member, member.SpanStart) - 1)
			.Any(string.IsNullOrWhiteSpace);
	}

	/// <summary>
	/// Tells whether no blank line stands between two neighbours above the summary of the second, where no region
	/// boundary parts them.
	/// </summary>
	private static bool IsTouching((SyntaxNode Previous, SyntaxNode Next) neighbours)
	{
		if (neighbours.Next.GetLeadingTrivia().Any(static x => x.IsDirective))
		{
			return false;
		}

		return !ReadLines(neighbours.Next, GetEndLine(neighbours.Previous) + 1, GetSummaryLine(neighbours.Next) - 1)
			.Any(string.IsNullOrWhiteSpace);
	}

	/// <summary>
	/// Reads the lines of the file of a member from the first to the last, both included.
	/// </summary>
	private static IEnumerable<string> ReadLines(SyntaxNode member, int first, int last)
	{
		TextLineCollection lines = member
			.SyntaxTree
			.GetText()
			.Lines;

		for (int i = first; i <= last; i++)
		{
			yield return lines[i].ToString();
		}
	}
	#endregion
}
