using DataOrganizer.GuardTests.TestHelpers.Dto;
using DataOrganizer.TestSupport.Guards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.GuardTests.TestHelpers;

/// <summary>
/// The types the test projects declare, read from their source: what their constructors take, what they implement and
/// what their static methods return.
/// </summary>
internal static partial class TestTypeIndex
{
	#region Data
	/// <summary>
	/// The declarations of all test sources, read once.
	/// </summary>
	private static readonly Lazy<Declarations> Index = new(Load);
	#endregion

	#region Methods
	/// <summary>
	/// Tells whether the test sources declare a type of a name.
	/// </summary>
	public static bool Contains(string name) => Index.Value.Constructors.ContainsKey(name);

	/// <summary>
	/// Enumerates the types a test type of a name derives from or implements, as its declaration writes them.
	/// </summary>
	public static IEnumerable<string> GetBases(string name) => Index.Value.Bases.GetValueOrDefault(name) ?? [];

	/// <summary>
	/// Enumerates the parameter types of the constructors of a test type of a name.
	/// </summary>
	public static IEnumerable<string> GetConstructorParameters(string name)
	{
		return Index
			.Value
			.Constructors
			.GetValueOrDefault(name)?
			.SelectMany(static x => x) ?? [];
	}

	/// <summary>
	/// Gets the type a static method of a test type returns, written as <c>Type.Method</c>, or <c>null</c>.
	/// </summary>
	public static string? GetReturnType(string typeAndMethod) => Index.Value.Returns.GetValueOrDefault(typeAndMethod);

	/// <summary>
	/// Tells whether a test type of a name is a class the container can build.
	/// </summary>
	public static bool IsConcreteClass(string name) => Index.Value.ConcreteClasses.Contains(name);
	#endregion

	#region Helpers
	/// <summary>
	/// Matches a constructor and its parameter list.
	/// </summary>
	[GeneratedRegex(@"\b(?:public|internal|protected|private)\s+(?<name>\w+)\s*\((?<parameters>[^)]*)\)\s*(?::\s*(?:base|this)\s*\([^)]*\)\s*)?\{")]
	private static partial Regex ConstructorRegex();

	/// <summary>
	/// Matches the declaration of a class or a record, with its primary constructor and its base list.
	/// </summary>
	[GeneratedRegex(@"(?<modifiers>(?:\b(?:public|internal|private|protected|sealed|static|abstract|partial|file)\s+)*)\b(?:class|record)\s+(?<name>\w+)(?:\s*<[^>{(]*>)?(?:\s*\((?<primary>[^)]*)\))?(?:\s*:\s*(?<bases>[^{;]+))?")]
	private static partial Regex DeclarationRegex();

	/// <summary>
	/// Reads the declarations of every test source.
	/// </summary>
	private static Declarations Load()
	{
		Declarations declarations = new();

		foreach ((_, string text) in RepositoryFiles.EnumerateTestCode())
		{
			Read(CodeText.WithoutCommentsAndLiterals(text), declarations);
		}

		return declarations;
	}

	/// <summary>
	/// Matches the attributes and the modifiers of a parameter.
	/// </summary>
	[GeneratedRegex(@"\[[^\]]*\]|\b(?:params|ref|out|in|this|scoped|readonly)\b")]
	private static partial Regex ParameterNoiseRegex();

	/// <summary>
	/// Reads the declarations of the types of a source.
	/// </summary>
	private static void Read(string code, Declarations declarations)
	{
		Match[] types = [.. DeclarationRegex().Matches(code).AsEnumerable()];

		foreach (Match type in types)
		{
			string name = type
				.Groups["name"]
				.Value;

			string modifiers = type
				.Groups["modifiers"]
				.Value;

			List<string[]> constructors = declarations.Constructors.TryGetValue(name, out List<string[]>? known) ? known : [];

			declarations.Constructors[name] = constructors;

			if (type.Groups["primary"].Success)
			{
				constructors.Add(ReadParameterTypes(type.Groups["primary"].Value));
			}

			if (type.Groups["bases"].Success)
			{
				declarations.Bases[name] = [.. TypeCatalog.SplitTopLevel(type.Groups["bases"].Value).Select(TypeCatalog.Normalize)];
			}

			if (!modifiers.Contains("static", StringComparison.Ordinal) && !modifiers.Contains("abstract", StringComparison.Ordinal))
			{
				declarations
					.ConcreteClasses
					.Add(name);
			}
		}

		foreach (Match constructor in ConstructorRegex().Matches(code))
		{
			if (declarations.Constructors.TryGetValue(constructor.Groups["name"].Value, out List<string[]>? constructors))
			{
				constructors.Add(ReadParameterTypes(constructor.Groups["parameters"].Value));
			}
		}

		foreach (Match method in StaticMethodRegex().Matches(code))
		{
			if (types.LastOrDefault(x => x.Index < method.Index) is { } owner)
			{
				declarations.Returns[$"{owner.Groups["name"].Value}.{method.Groups["name"].Value}"] = TypeCatalog.Normalize(method.Groups["type"].Value);
			}
		}
	}

	/// <summary>
	/// Reads the type of a parameter from its declaration: the words before its name, without modifiers or default.
	/// </summary>
	private static string? ReadParameterType(string parameter)
	{
		string text = ParameterNoiseRegex()
			.Replace(parameter.Split('=')[0], " ")
			.Trim();

		int space = text.LastIndexOf(' ');

		return space <= 0 ? null : TypeCatalog.Normalize(text[..space]);
	}

	/// <summary>
	/// Reads the parameter types of a parameter list.
	/// </summary>
	private static string[] ReadParameterTypes(string parameters)
	{
		return [.. TypeCatalog
			.SplitTopLevel(parameters)
			.Select(ReadParameterType)
			.OfType<string>()];
	}

	/// <summary>
	/// Matches a static method, its return type and its name.
	/// </summary>
	[GeneratedRegex(@"\bstatic\s+(?<type>[\w.]+(?:<[^()=;{}]*>)?(?:\[\])?\??)\s+(?<name>\w+)\s*(?:<[^()]*>)?\s*\(")]
	private static partial Regex StaticMethodRegex();
	#endregion
}
