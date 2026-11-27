using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DataOrganizer.GuardTests.TestHelpers;

/// <summary>
/// The types of the application and of the libraries it loads, found by the names a source writes.
/// </summary>
internal static partial class TypeCatalog
{
	#region Data
	/// <summary>
	/// The names of the types of C# keywords, as reflection gives them.
	/// </summary>
	private static readonly FrozenDictionary<string, string> KeywordTypes = new Dictionary<string, string>
	{
		["bool"] = nameof(Boolean),
		["byte"] = nameof(Byte),
		["char"] = nameof(Char),
		["decimal"] = nameof(Decimal),
		["double"] = nameof(Double),
		["float"] = nameof(Single),
		["int"] = nameof(Int32),
		["long"] = nameof(Int64),
		["object"] = nameof(Object),
		["string"] = nameof(String)
	}.ToFrozenDictionary();

	/// <summary>
	/// Every type of the application and of the loaded libraries, by its name without namespace and type arguments.
	/// </summary>
	private static readonly Lazy<ILookup<string, Type>> Types = new(LoadTypes);

	/// <summary>
	/// Wrappers through which a constructor takes the services of a container: their own name and the wrapped one count.
	/// </summary>
	private static readonly FrozenSet<string> Wrappers = FrozenSet.Create(
		"Func",
		"ICollection",
		"IEnumerable",
		"IList",
		"IReadOnlyCollection",
		"IReadOnlyList",
		"Lazy",
		"Owned");
	#endregion

	#region Methods
	/// <summary>
	/// Finds the types of a name written in a source.
	/// </summary>
	public static Type[] Find(string name) => [.. Types.Value[ToBaseName(name)]];

	/// <summary>
	/// Writes a type as <see cref="Normalize" /> writes it from a source.
	/// </summary>
	public static string Format(Type type)
	{
		if (type.IsArray)
		{
			return $"{Format(type.GetElementType()!)}[]";
		}

		if (Nullable.GetUnderlyingType(type) is { } underlying)
		{
			return Format(underlying);
		}

		string name = ToBaseName(type.Name);

		return type.IsGenericType
			? $"{name}<{string.Join(',', type.GetGenericArguments().Select(Format))}>"
			: name;
	}

	/// <summary>
	/// Enumerates the names of the interfaces a type of a name implements, an interface counting itself.
	/// </summary>
	public static IEnumerable<string> GetInterfaces(string name)
	{
		return Find(name)
			.SelectMany(static x => x.IsInterface ? [x, .. x.GetInterfaces()] : x.GetInterfaces())
			.Select(Format)
			.Distinct();
	}

	/// <summary>
	/// Gets the type of a property, field or method written as <c>Type.Member</c>, or <c>null</c> when there is none.
	/// </summary>
	public static string? GetMemberType(string expression)
	{
		if (MemberRegex().Match(expression) is not { Success: true } match)
		{
			return null;
		}

		Type? memberType = Find(match.Groups["type"].Value)
			.Select(x => GetMemberType(x, match.Groups["member"].Value))
			.FirstOrDefault(static x => x is not null);

		return memberType is null ? null : Format(memberType);
	}

	/// <summary>
	/// Enumerates the names of the services a parameter of a type takes: the type itself and, for a wrapper or an
	/// array, the type it wraps.
	/// </summary>
	public static IEnumerable<string> GetTakenServices(string parameterType)
	{
		yield return parameterType;

		if (parameterType.EndsWith("[]", StringComparison.Ordinal))
		{
			yield return parameterType[..^2];
		}
		else if (WrapperRegex().Match(parameterType) is { Success: true } wrapper && Wrappers.Contains(wrapper.Groups["wrapper"].Value))
		{
			yield return SplitTopLevel(wrapper.Groups["arguments"].Value).First();
		}
	}

	/// <summary>
	/// Tells whether a name stands for a class the container can build without a registration.
	/// </summary>
	public static bool IsConcreteClass(string name) => Find(name).Any(static x => x.IsClass && !x.IsAbstract);

	/// <summary>
	/// Writes a type as a source names it, without spaces, namespaces and nullable marks, keywords as type names.
	/// </summary>
	public static string Normalize(string typeText)
	{
		string text = typeText.Replace("global::", string.Empty, StringComparison.Ordinal);

		text = QualifierRegex().Replace(text, string.Empty);

		text = string.Concat(text.Where(static x => !char.IsWhiteSpace(x) && x != '?'));

		return KeywordRegex().Replace(text, static x => KeywordTypes[x.Value]);
	}

	/// <summary>
	/// Splits a list at its top-level commas, those outside angle brackets, parentheses and square brackets.
	/// </summary>
	public static IEnumerable<string> SplitTopLevel(string list)
	{
		int depth = 0;

		int start = 0;

		for (int i = 0; i < list.Length; i++)
		{
			depth += list[i] switch
			{
				'<' or '(' or '[' => 1,
				'>' or ')' or ']' => -1,
				_ => 0
			};

			if (depth == 0 && list[i] == ',')
			{
				yield return list[start..i];

				start = i + 1;
			}
		}

		yield return list[start..];
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Gets the type of a property, field or method of a type, or <c>null</c> when it has none of that name.
	/// </summary>
	private static Type? GetMemberType(Type type, string member)
	{
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

		return type.GetProperty(member, flags)?.PropertyType
			?? type.GetField(member, flags)?.FieldType
			?? type.GetMethods(flags).FirstOrDefault(x => x.Name == member)?.ReturnType;
	}

	/// <summary>
	/// Matches a C# keyword that names a type.
	/// </summary>
	[GeneratedRegex(@"\b(?:bool|byte|char|decimal|double|float|int|long|object|string)\b")]
	private static partial Regex KeywordRegex();

	/// <summary>
	/// Loads an assembly the application references. One the output of the guards does not carry, as a library of
	/// another platform, adds no types.
	/// </summary>
	private static void Load(AssemblyName name)
	{
		try
		{
			_ = Assembly.Load(name);
		}
		catch (Exception exception) when (exception is IOException or BadImageFormatException)
		{
		}
	}

	/// <summary>
	/// Loads every type of the application and of the libraries it references.
	/// </summary>
	private static ILookup<string, Type> LoadTypes()
	{
		foreach (AssemblyName reference in AppAssemblies.Enumerate().SelectMany(static x => x.GetReferencedAssemblies()))
		{
			Load(reference);
		}

		return AppDomain
			.CurrentDomain
			.GetAssemblies()
			.Where(static x => !x.IsDynamic)
			.SelectMany(LoadTypes)
			.ToLookup(static x => ToBaseName(x.Name));
	}

	/// <summary>
	/// Loads the types of an assembly, those it can load when some of them cannot be.
	/// </summary>
	private static Type[] LoadTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException exception)
		{
			return [.. exception.Types.OfType<Type>()];
		}
	}

	/// <summary>
	/// Matches a member written after its type.
	/// </summary>
	[GeneratedRegex(@"^(?<type>[A-Z]\w*)\.(?<member>[A-Z]\w*)$")]
	private static partial Regex MemberRegex();

	/// <summary>
	/// Matches the namespace or the containing type before a name.
	/// </summary>
	[GeneratedRegex(@"\b(?:[A-Za-z_]\w*\.)+(?=[A-Za-z_])")]
	private static partial Regex QualifierRegex();

	/// <summary>
	/// Cuts the arity and the type arguments from a name.
	/// </summary>
	private static string ToBaseName(string name) => name.Split('`', '<')[0];

	/// <summary>
	/// Matches a generic type with its name and its type arguments.
	/// </summary>
	[GeneratedRegex(@"^(?<wrapper>\w+)<(?<arguments>.+)>$")]
	private static partial Regex WrapperRegex();
	#endregion
}
