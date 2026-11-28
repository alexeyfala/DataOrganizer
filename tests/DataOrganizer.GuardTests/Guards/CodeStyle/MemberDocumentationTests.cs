using AwesomeAssertions;
using DataOrganizer.GuardTests.TestHelpers;
using DataOrganizer.TestSupport.Guards;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.GuardTests.Guards.CodeStyle;

/// <summary>
/// Guards the documentation that StyleCop leaves unchecked: the parts of partial members and the explicit interface
/// implementations.
/// </summary>
[Guard]
[TestFixture]
internal sealed class MemberDocumentationTests
{
	#region Methods
	/// <summary>
	/// Every explicit interface implementation carries a summary or <c>inheritdoc</c>; StyleCop does not ask for one.
	/// </summary>
	[Test]
	public void Explicit_Implementations_Carry_A_Summary()
	{
		// Act
		MemberDeclarationSyntax[] members = [.. EnumerateMembers()];

		string[] undocumented =
		[
			.. members
				.Where(static x => IsExplicitImplementation(x) && !IsDocumented(x))
				.Select(Describe)
		];

		// Assert
		members
			.Should()
			.NotBeEmpty();

		undocumented
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// Every hand-written part of a partial type or member carries its own summary or <c>inheritdoc</c>; StyleCop takes
	/// a part as documented when another part is, a generated one included.
	/// </summary>
	[Test]
	public void Partial_Parts_Carry_Their_Own_Summary()
	{
		// Act
		MemberDeclarationSyntax[] members = [.. EnumerateMembers()];

		string[] undocumented =
		[
			.. members
				.Where(static x => x.Modifiers.Any(SyntaxKind.PartialKeyword) && !IsDocumented(x))
				.Select(Describe)
		];

		// Assert
		members
			.Should()
			.NotBeEmpty();

		undocumented
			.Should()
			.HaveCount(0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Describes a member by its file, its line and its name.
	/// </summary>
	private static string Describe(MemberDeclarationSyntax member)
	{
		int line = member
			.GetLocation()
			.GetLineSpan()
			.StartLinePosition
			.Line;

		return $"{member.SyntaxTree.FilePath}:{line + 1} {CodeMaidMemberOrder.GetName(member)}";
	}

	/// <summary>
	/// Enumerates the types and members of every hand-written source file.
	/// </summary>
	private static IEnumerable<MemberDeclarationSyntax> EnumerateMembers()
	{
		return SourceSyntax
			.GetTrees()
			.SelectMany(static x => x.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>());
	}

	/// <summary>
	/// Gets the name of an element of a documentation comment, or an empty name for text.
	/// </summary>
	private static string GetElementName(XmlNodeSyntax node)
	{
		return node switch
		{
			XmlElementSyntax x => x.StartTag.Name.LocalName.Text,
			XmlEmptyElementSyntax x => x.Name.LocalName.Text,
			_ => string.Empty
		};
	}

	/// <summary>
	/// Tells whether the documentation comment of a member holds a summary or <c>inheritdoc</c>.
	/// </summary>
	private static bool IsDocumented(MemberDeclarationSyntax member)
	{
		return member
			.GetLeadingTrivia()
			.Select(static x => x.GetStructure())
			.OfType<DocumentationCommentTriviaSyntax>()
			.SelectMany(static x => x.Content)
			.Any(static x => GetElementName(x) is "summary" or "inheritdoc");
	}

	/// <summary>
	/// Tells whether a member implements a member of an interface explicitly.
	/// </summary>
	private static bool IsExplicitImplementation(MemberDeclarationSyntax member)
	{
		ExplicitInterfaceSpecifierSyntax? specifier = member switch
		{
			BasePropertyDeclarationSyntax x => x.ExplicitInterfaceSpecifier,
			ConversionOperatorDeclarationSyntax x => x.ExplicitInterfaceSpecifier,
			MethodDeclarationSyntax x => x.ExplicitInterfaceSpecifier,
			OperatorDeclarationSyntax x => x.ExplicitInterfaceSpecifier,
			_ => null
		};

		return specifier is not null;
	}
	#endregion
}
