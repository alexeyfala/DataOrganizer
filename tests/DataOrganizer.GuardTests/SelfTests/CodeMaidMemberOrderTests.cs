using AwesomeAssertions;
using DataOrganizer.GuardTests.TestHelpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.GuardTests.SelfTests;

/// <summary>
/// Tests of <see cref="CodeMaidMemberOrder" /> type.
/// </summary>
[TestFixture]
internal sealed class CodeMaidMemberOrderTests
{
	#region Methods
	/// <summary>
	/// <see cref="CodeMaidMemberOrder.Compare" />: two members of a class come out in the order of CodeMaid, by kind,
	/// access, const, static and readonly before the name; a static constructor and an explicit implementation count
	/// as public.
	/// </summary>
	[TestCase("private int _a;", "public Sut() { }")]
	[TestCase("public Sut() { }", "~Sut() { }")]
	[TestCase("~Sut() { }", "public delegate void Handler();")]
	[TestCase("public delegate void Handler();", "public event System.Action? Changed;")]
	[TestCase("public event System.Action? Changed;", "public enum Kind { A }")]
	[TestCase("public enum Kind { A }", "public interface IPart { }")]
	[TestCase("public interface IPart { }", "public int Count { get; }")]
	[TestCase("public int Count { get; }", "public int this[int i] => i;")]
	[TestCase("public int this[int i] => i;", "public void Run() { }")]
	[TestCase("public void Run() { }", "public struct Point { }")]
	[TestCase("public struct Point { }", "public class Nested { }")]
	[TestCase("public void B() { }", "internal void A() { }")]
	[TestCase("static Sut() { }", "public Sut() { }")]
	[TestCase("void System.IDisposable.Dispose() { }", "internal void A() { }")]
	[TestCase("internal void B() { }", "private protected void A() { }")]
	[TestCase("internal void B() { }", "protected void A() { }")]
	[TestCase("protected void B() { }", "private void A() { }")]
	[TestCase("private const int B = 1;", "private static readonly int A = 1;")]
	[TestCase("private static readonly int B = 1;", "private static int A;")]
	[TestCase("private static int B;", "private readonly int A;")]
	[TestCase("private readonly int B;", "private int A;")]
	[TestCase("private static void B() { }", "private void A() { }")]
	[TestCase("public void Apply_X() { }", "public void ApplyAsync_X() { }")]
	public void Compare_Puts_Members_In_The_Order_Of_CodeMaid(string first, string second)
	{
		// Arrange
		MemberDeclarationSyntax[] members = ParseMembers(second, first);

		// Act
		MemberDeclarationSyntax[] sorted = [.. members.Order(Comparer<MemberDeclarationSyntax>.Create(CodeMaidMemberOrder.Compare))];

		// Assert
		sorted.Select(static x => x.ToString())
			.Should()
			.Equal(first, second);
	}

	/// <summary>
	/// <see cref="CodeMaidMemberOrder.CompareNames" />: names come out in the order of CodeMaid under the en-US culture:
	/// the case ignored first, <c>_</c> before digits before letters, then a lowercase letter before its capital.
	/// </summary>
	[TestCase("Apply_Sets", "ApplyAsync_Sets")]
	[TestCase("Item1", "ItemA")]
	[TestCase("Label", "LabelSize")]
	[TestCase("_shown", "_showNextAt")]
	[TestCase("alpha", "Beta")]
	[TestCase("item", "Item")]
	public void CompareNames_Puts_Names_In_The_Order_Of_CodeMaid(string first, string second)
	{
		// Arrange
		string[] names = [second, first];

		// Act
		string[] sorted = [.. names.Order(Comparer<string>.Create(CodeMaidMemberOrder.CompareNames))];

		// Assert
		sorted
			.Should()
			.Equal(first, second);
	}

	/// <summary>
	/// <see cref="CodeMaidMemberOrder.SortsMembersOf" />: CodeMaid keeps the order of a type with a layout attribute and
	/// of the types inside it.
	/// </summary>
	[TestCase("class Sut { }", true)]
	[TestCase("[StructLayout(LayoutKind.Sequential)] struct Sut { }", false)]
	[TestCase("[System.Runtime.InteropServices.StructLayoutAttribute(LayoutKind.Sequential)] struct Sut { }", false)]
	[TestCase("[ComImport, Guid(\"0\")] interface Sut { }", false)]
	[TestCase("[StructLayout(LayoutKind.Sequential)] struct Outer { class Sut { } }", false)]
	public void SortsMembersOf_Leaves_Out_A_Layout_Type_And_The_Types_Inside_It(string code, bool expected)
	{
		// Arrange
		TypeDeclarationSyntax type = CSharpSyntaxTree
			.ParseText(code)
			.GetRoot()
			.DescendantNodes()
			.OfType<TypeDeclarationSyntax>()
			.Single(static x => x.Identifier.Text == "Sut");

		// Act
		bool result = CodeMaidMemberOrder.SortsMembersOf(type);

		// Assert
		result
			.Should()
			.Be(expected);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Parses members as the members of a class named <c>Sut</c>, in the given order.
	/// </summary>
	private static MemberDeclarationSyntax[] ParseMembers(params string[] members)
	{
		return
		[
			.. CSharpSyntaxTree
				.ParseText($"class Sut {{ {string.Join(' ', members)} }}")
				.GetRoot()
				.DescendantNodes()
				.OfType<ClassDeclarationSyntax>()
				.First()
				.Members
		];
	}
	#endregion
}
