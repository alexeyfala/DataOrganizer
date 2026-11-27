using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace DataOrganizer.GuardTests.TestHelpers;

/// <summary>
/// The order in which the Reorganize command of CodeMaid (Ctrl+M, Z) with its default settings puts the members of a
/// type: the algorithm of its <c>CodeItemTypeComparer</c>, repeated on the syntax tree.
/// </summary>
internal static class CodeMaidMemberOrder
{
	#region Data
	/// <summary>
	/// Attributes of a type whose members CodeMaid keeps in the order of the file: a memory layout or a vtable.
	/// </summary>
	private static readonly string[] KeepOrderAttributes = ["ComImport", "StructLayout"];
	#endregion

	#region Methods
	/// <summary>
	/// Compares two members of one type: below zero when CodeMaid puts the first above the second, above zero when below.
	/// </summary>
	public static int Compare(MemberDeclarationSyntax x, MemberDeclarationSyntax y)
	{
		int byGroup = GetGroup(x).CompareTo(GetGroup(y));

		return byGroup != 0 ? byGroup : CompareNames(GetName(x), GetName(y));
	}

	/// <summary>
	/// Compares two names as CodeMaid does under the en-US culture of Visual Studio: the case ignored first, <c>_</c>
	/// before digits and digits before letters, then a lowercase letter before its capital.
	/// </summary>
	public static int CompareNames(string x, string y)
	{
		for (int i = 0; i < Math.Min(x.Length, y.Length); i++)
		{
			int byWeight = GetWeight(x[i]).CompareTo(GetWeight(y[i]));

			if (byWeight != 0)
			{
				return byWeight;
			}
		}

		if (x.Length != y.Length)
		{
			return x.Length.CompareTo(y.Length);
		}

		for (int i = 0; i < x.Length; i++)
		{
			if (x[i] != y[i])
			{
				return char.IsLower(x[i]) ? -1 : 1;
			}
		}

		return 0;
	}

	/// <summary>
	/// Gets the name CodeMaid sorts a member by: an explicit implementation without its interface.
	/// </summary>
	public static string GetName(MemberDeclarationSyntax member)
	{
		return member switch
		{
			BaseFieldDeclarationSyntax x => x.Declaration.Variables[0].Identifier.Text,
			BaseTypeDeclarationSyntax x => x.Identifier.Text,
			ConstructorDeclarationSyntax x => x.Identifier.Text,
			ConversionOperatorDeclarationSyntax x => $"operator {x.Type}",
			DelegateDeclarationSyntax x => x.Identifier.Text,
			DestructorDeclarationSyntax x => x.Identifier.Text,
			EventDeclarationSyntax x => x.Identifier.Text,
			IndexerDeclarationSyntax => "this",
			MethodDeclarationSyntax x => x.Identifier.Text,
			OperatorDeclarationSyntax x => $"operator {x.OperatorToken.Text}",
			PropertyDeclarationSyntax x => x.Identifier.Text,
			_ => string.Empty
		};
	}

	/// <summary>
	/// Tells whether CodeMaid sorts the members of a type: it keeps the order of a type with a layout attribute and of
	/// the types inside it.
	/// </summary>
	public static bool SortsMembersOf(TypeDeclarationSyntax type)
	{
		return !type
			.AncestorsAndSelf()
			.OfType<TypeDeclarationSyntax>()
			.SelectMany(static x => x.AttributeLists)
			.SelectMany(static x => x.Attributes)
			.Any(static x => KeepOrderAttributes.Contains(GetAttributeName(x)));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Gets the place of the access of a member: public, internal, protected internal, protected, private. CodeMaid counts
	/// a static constructor and an explicit implementation as public; Visual Studio reads private protected as protected.
	/// </summary>
	private static int GetAccessOrder(MemberDeclarationSyntax member)
	{
		SyntaxTokenList modifiers = member.Modifiers;

		bool isPublicForCodeMaid = (member is ConstructorDeclarationSyntax && modifiers.Any(SyntaxKind.StaticKeyword))
			|| member is MethodDeclarationSyntax { ExplicitInterfaceSpecifier: not null }
			|| member is BasePropertyDeclarationSyntax { ExplicitInterfaceSpecifier: not null };

		if (isPublicForCodeMaid || modifiers.Any(SyntaxKind.PublicKeyword))
		{
			return 1;
		}

		bool isProtected = modifiers.Any(SyntaxKind.ProtectedKeyword);
		bool isInternal = modifiers.Any(SyntaxKind.InternalKeyword);

		if (isProtected && isInternal)
		{
			return 4;
		}

		if (isInternal)
		{
			return 3;
		}

		if (isProtected)
		{
			return 5;
		}

		// Without a modifier, Visual Studio reads a field, method or property of an interface as public.
		bool isInterfaceMember = !modifiers.Any(SyntaxKind.PrivateKeyword)
			&& member is BaseFieldDeclarationSyntax or BaseMethodDeclarationSyntax or BasePropertyDeclarationSyntax
			&& member.Ancestors().OfType<InterfaceDeclarationSyntax>().Any();

		return isInterfaceMember ? 1 : 6;
	}

	/// <summary>
	/// Gets the name of an attribute without its namespace and its <c>Attribute</c> suffix.
	/// </summary>
	private static string GetAttributeName(AttributeSyntax attribute)
	{
		string name = attribute.Name switch
		{
			QualifiedNameSyntax x => x.Right.Identifier.Text,
			SimpleNameSyntax x => x.Identifier.Text,
			_ => attribute.Name.ToString()
		};

		return name.EndsWith("Attribute", StringComparison.Ordinal) ? name[..^"Attribute".Length] : name;
	}

	/// <summary>
	/// Gets the number CodeMaid sorts a member by before its name: its kind, its access, then const, static and readonly
	/// first.
	/// </summary>
	private static int GetGroup(MemberDeclarationSyntax member)
	{
		SyntaxTokenList modifiers = member.Modifiers;

		bool isField = member is FieldDeclarationSyntax;
		bool isConstant = modifiers.Any(SyntaxKind.ConstKeyword);
		bool isStatic = isConstant || modifiers.Any(SyntaxKind.StaticKeyword);
		bool isReadOnly = modifiers.Any(SyntaxKind.ReadOnlyKeyword);

		int constantOffset = isField && !isConstant ? 1 : 0;
		int staticOffset = isStatic ? 0 : 1;
		int readOnlyOffset = isField && !isReadOnly ? 1 : 0;

		return (GetKindOrder(member) * 100000)
			+ (GetAccessOrder(member) * 10000)
			+ (constantOffset * 100)
			+ (staticOffset * 10)
			+ readOnlyOffset;
	}

	/// <summary>
	/// Gets the place of the kind of a member: fields, constructors, finalizers, delegates, events, enums, interfaces,
	/// properties, indexers, methods, structs, classes.
	/// </summary>
	private static int GetKindOrder(MemberDeclarationSyntax member)
	{
		return member switch
		{
			FieldDeclarationSyntax => 1,
			ConstructorDeclarationSyntax => 2,
			DestructorDeclarationSyntax => 3,
			DelegateDeclarationSyntax => 4,
			EventFieldDeclarationSyntax or EventDeclarationSyntax => 5,
			EnumDeclarationSyntax => 6,
			InterfaceDeclarationSyntax => 7,
			PropertyDeclarationSyntax => 8,
			IndexerDeclarationSyntax => 9,
			MethodDeclarationSyntax or OperatorDeclarationSyntax or ConversionOperatorDeclarationSyntax => 10,
			StructDeclarationSyntax or RecordDeclarationSyntax { ClassOrStructKeyword.Text: "struct" } => 11,
			ClassDeclarationSyntax or RecordDeclarationSyntax => 12,
			_ => 0
		};
	}

	/// <summary>
	/// Gets the weight of a character of a name with its case ignored: <c>_</c>, then digits, then letters.
	/// </summary>
	private static int GetWeight(char c)
	{
		return c switch
		{
			'_' => 0,
			_ when char.IsAsciiDigit(c) => 1 + (c - '0'),
			_ when char.IsAsciiLetter(c) => 11 + (char.ToLowerInvariant(c) - 'a'),
			_ => 100 + c
		};
	}
	#endregion
}
