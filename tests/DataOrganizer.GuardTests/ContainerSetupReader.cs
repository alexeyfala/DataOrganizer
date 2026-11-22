using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.GuardTests;

/// <summary>
/// Reads from the source of a test file the containers its tests set up with <c>AutoMock.GetLoose</c>.
/// </summary>
internal static partial class ContainerSetupReader
{
	#region Methods
	/// <summary>
	/// Reads the containers the tests of a file set up with registrations.
	/// </summary>
	public static IEnumerable<ContainerSetup> Read(string filePath, string text)
	{
		string code = CodeText.WithoutCommentsAndLiterals(text);

		Match[] methods = [.. MethodRegex().Matches(code).AsEnumerable()];

		foreach (Match setup in SetupRegex().Matches(code))
		{
			Match? method = methods.LastOrDefault(x => x.Index < setup.Index);

			if (method is null)
			{
				continue;
			}

			int methodEnd = methods.FirstOrDefault(x => x.Index > setup.Index)?.Index ?? code.Length;

			string methodCode = code[method.Index..methodEnd];

			int open = setup.Groups["open"].Index;

			string lambda = code[open..(FindClosing(code, open) + 1)];

			yield return new ContainerSetup
			{
				BuiltTypes = [.. ReadBuiltTypes(methodCode, setup.Groups["mock"].Value)],
				FilePath = filePath,
				Method = method.Groups["name"].Value,
				Registrations = [.. ReadRegistrations(lambda, setup.Groups["builder"].Value, methodCode, code)]
			};
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Matches a registration exposed as the interfaces of its type.
	/// </summary>
	[GeneratedRegex(@"\.\s*AsImplementedInterfaces\s*\(")]
	private static partial Regex AsImplementedInterfacesRegex();

	/// <summary>
	/// Matches a service a registration is exposed as.
	/// </summary>
	[GeneratedRegex(@"\.\s*As\s*<(?<types>[^()]*?)>\s*\(\s*\)")]
	private static partial Regex AsRegex();

	/// <summary>
	/// Matches a registration exposed as its own type.
	/// </summary>
	[GeneratedRegex(@"\.\s*AsSelf\s*\(")]
	private static partial Regex AsSelfRegex();

	/// <summary>
	/// Matches a call that builds a type of a container.
	/// </summary>
	[GeneratedRegex(@"\b(?<receiver>\w+)\s*\.\s*(?<kind>Create|Resolve)\s*<(?<type>[^()]*?)>\s*\(")]
	private static partial Regex BuildRegex();

	/// <summary>
	/// Finds the closing parenthesis that matches the opening one at an index.
	/// </summary>
	private static int FindClosing(string text, int open)
	{
		int depth = 0;

		for (int i = open; i < text.Length; i++)
		{
			depth += text[i] switch
			{
				'(' => 1,
				')' => -1,
				_ => 0
			};

			if (depth == 0)
			{
				return i;
			}
		}

		return text.Length - 1;
	}

	/// <summary>
	/// Finds the end of the statement that starts at an index: its semicolon, or the end of the enclosing block.
	/// </summary>
	private static int FindStatementEnd(string text, int start)
	{
		int depth = 0;

		for (int i = start; i < text.Length; i++)
		{
			depth += text[i] switch
			{
				'(' or '{' => 1,
				')' or '}' => -1,
				_ => 0
			};

			if (depth < 0 || (depth == 0 && text[i] == ';'))
			{
				return i;
			}
		}

		return text.Length;
	}

	/// <summary>
	/// Matches a member of a variable.
	/// </summary>
	[GeneratedRegex(@"^(?<variable>[a-z_]\w*)\.(?<member>[A-Z]\w*)$")]
	private static partial Regex InstanceMemberRegex();

	/// <summary>
	/// Matches a lambda with one parameter and an expression body.
	/// </summary>
	[GeneratedRegex(@"^\s*\w+\s*=>\s*(?<body>[\s\S]+?)\s*$")]
	private static partial Regex LambdaRegex();

	/// <summary>
	/// Matches the header of a member of a class, with its name.
	/// </summary>
	[GeneratedRegex(@"^\t(?:public|private|internal|protected)\b[^\r\n]*?\b(?<name>\w+)\s*(?:<[^\r\n]*?>)?\s*\(", RegexOptions.Multiline)]
	private static partial Regex MethodRegex();

	/// <summary>
	/// Matches an instance built with <c>new</c> and its type.
	/// </summary>
	[GeneratedRegex(@"^new\s+(?<type>[\w.]+(?:<[^()]*>)?)\s*[({]")]
	private static partial Regex NewRegex();

	/// <summary>
	/// Reads the types a test builds from its container: those it creates from the mock and those it resolves.
	/// </summary>
	private static IEnumerable<string> ReadBuiltTypes(string methodCode, string mock)
	{
		return BuildRegex()
			.Matches(methodCode)
			.Where(x => x.Groups["kind"].Value == "Resolve" || x.Groups["receiver"].Value == mock)
			.Select(static x => TypeCatalog.Normalize(x.Groups["type"].Value))
			.Distinct();
	}

	/// <summary>
	/// Reads the declared type of a variable, a parameter or a field, looking in the method first.
	/// </summary>
	private static string? ReadDeclaredType(string name, string methodCode, string code)
	{
		Regex declaration = new($@"(?<type>[A-Za-z_][\w.]*(?:<[^;=(){{}}]*?>)?(?:\[\])?\??)\s+{Regex.Escape(name)}\s*(?:=|;|,|\))");

		Match? match = declaration.Matches(methodCode).LastOrDefault() ?? declaration.Matches(code).LastOrDefault();

		return match is null || match.Groups["type"].Value is "var" or "return" or "out" or "ref" or "in"
			? null
			: TypeCatalog.Normalize(match.Groups["type"].Value);
	}

	/// <summary>
	/// Reads the type of the instance an expression gives to a registration, or <c>null</c> when the source does not
	/// show it.
	/// </summary>
	private static string? ReadInstanceType(string expression, string methodCode, string code)
	{
		string text = expression.Trim().TrimEnd('!');

		if (SubstituteRegex().Match(text) is { Success: true } substitute)
		{
			return SplitTypeList(substitute.Groups["types"].Value).First();
		}

		if (NewRegex().Match(text) is { Success: true } created)
		{
			return TypeCatalog.Normalize(created.Groups["type"].Value);
		}

		if (text.All(static x => char.IsLetterOrDigit(x) || x == '_'))
		{
			return ReadDeclaredType(text, methodCode, code);
		}

		if (StaticMemberRegex().Match(text) is { Success: true } member)
		{
			return KnownTypes.GetMemberType($"{member.Groups["type"].Value}.{member.Groups["member"].Value}");
		}

		if (InstanceMemberRegex().Match(text) is { Success: true } access
			&& ReadDeclaredType(access.Groups["variable"].Value, methodCode, code) is { } variableType)
		{
			return KnownTypes.GetMemberType($"{variableType}.{access.Groups["member"].Value}");
		}

		return null;
	}

	/// <summary>
	/// Reads the type of the instance a delegate registration returns, or <c>null</c> when the source does not show it.
	/// </summary>
	private static string? ReadLambdaResultType(string lambda, string methodCode, string code)
	{
		return LambdaRegex().Match(lambda) is { Success: true } match
			? ReadInstanceType(match.Groups["body"].Value, methodCode, code)
			: null;
	}

	/// <summary>
	/// Reads the registrations a test makes on the builder of its container.
	/// </summary>
	private static IEnumerable<ContainerRegistration> ReadRegistrations(
		string lambda,
		string builder,
		string methodCode,
		string code)
	{
		foreach (Match registration in RegistrationRegex().Matches(lambda))
		{
			if (registration.Groups["builder"].Value != builder)
			{
				continue;
			}

			int open = registration.Groups["open"].Index;

			int close = FindClosing(lambda, open);

			string argument = lambda[(open + 1)..close];

			string chain = lambda[(close + 1)..FindStatementEnd(lambda, close + 1)];

			string kind = registration.Groups["kind"].Value;

			string? generic = registration.Groups["generic"].Success ? TypeCatalog.Normalize(registration.Groups["generic"].Value) : null;

			string? implementation = kind == "RegisterType" ? generic : null;

			string? provided = kind switch
			{
				"RegisterType" => generic,
				"RegisterInstance" => generic ?? ReadInstanceType(argument, methodCode, code),
				_ => generic ?? ReadLambdaResultType(argument, methodCode, code)
			};

			yield return new ContainerRegistration
			{
				Implementation = implementation,
				Services = ReadServices(chain, provided),
				Text = WhitespaceRegex().Replace($"{kind}{(generic is null ? string.Empty : $"<{generic}>")}({argument.Trim()})", " ")
			};
		}
	}

	/// <summary>
	/// Reads the services a registration provides from its chain, or <c>null</c> when the source does not show them.
	/// </summary>
	private static string[]? ReadServices(string chain, string? provided)
	{
		List<string> services = [.. AsRegex().Matches(chain).SelectMany(static x => SplitTypeList(x.Groups["types"].Value))];

		bool isSelf = AsSelfRegex().IsMatch(chain);

		bool isImplemented = AsImplementedInterfacesRegex().IsMatch(chain);

		if (provided is null)
		{
			return isSelf || isImplemented || services.Count == 0 ? null : [.. services];
		}

		if (isSelf || (services.Count == 0 && !isImplemented))
		{
			services.Add(provided);
		}

		if (isImplemented)
		{
			services.AddRange(KnownTypes.GetInterfaces(provided));
		}

		return services.Count == 0 ? null : [.. services];
	}

	/// <summary>
	/// Matches a registration call on a builder.
	/// </summary>
	[GeneratedRegex(@"\b(?<builder>\w+)\s*\.\s*(?<kind>RegisterInstance|RegisterType|Register)\s*(?:<(?<generic>[^()]*?)>)?\s*(?<open>\()")]
	private static partial Regex RegistrationRegex();

	/// <summary>
	/// Matches a container a test sets up with registrations, with the names of the mock and of the builder.
	/// </summary>
	[GeneratedRegex(@"\b(?:AutoMock|var)\s+(?<mock>\w+)\s*=\s*AutoMock\s*\.\s*GetLoose\s*(?<open>\()\s*(?<builder>\w+)\s*=>")]
	private static partial Regex SetupRegex();

	/// <summary>
	/// Splits a list of type arguments into normalized type names.
	/// </summary>
	private static IEnumerable<string> SplitTypeList(string types) => TypeCatalog.SplitTopLevel(types).Select(TypeCatalog.Normalize);

	/// <summary>
	/// Matches a static member of a type, a call or not.
	/// </summary>
	[GeneratedRegex(@"^(?<type>[A-Z]\w*)\.(?<member>[A-Z]\w*)\s*(?:<[^()]*>)?\s*(?:\([\s\S]*\))?$")]
	private static partial Regex StaticMemberRegex();

	/// <summary>
	/// Matches a substitute and the types it stands for.
	/// </summary>
	[GeneratedRegex(@"^Substitute\s*\.\s*(?:For|ForPartsOf)\s*<(?<types>[^()]*?)>\s*\(")]
	private static partial Regex SubstituteRegex();

	/// <summary>
	/// Matches a run of whitespace.
	/// </summary>
	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();
	#endregion
}
