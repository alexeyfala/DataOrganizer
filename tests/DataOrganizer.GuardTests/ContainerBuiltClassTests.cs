using Avalonia;
using AwesomeAssertions;
using DataOrganizer.TestSupport.Guards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DataOrganizer.GuardTests;

[Guard]
[TestFixture(Description = "Guards the tests against building with new a class that the container of the application builds")]
internal partial class ContainerBuiltClassTests
{
	#region Data
	/// <summary>
	/// Name of the method the compiler gives every record for its <c>with</c> expression.
	/// </summary>
	private const string RecordCloneMethodName = "<Clone>$";
	#endregion

	#region Methods
	/// <summary>
	/// No test builds with <c>new</c> and arguments a class whose constructor takes a service, unless the application
	/// builds it with <c>new</c> too or it is a record or an object of the interface: the container builds it, so a
	/// test does too.
	/// </summary>
	[Test]
	public void Tests_Build_The_Classes_Of_The_Container_With_A_Container()
	{
		// Arrange
		HashSet<string> containerBuilt = [.. FindContainerBuiltClasses()];

		// Act
		string[] builtWithNew =
		[
			.. RepositoryFiles
				.EnumerateTestCode()
				.SelectMany(x => FindBuiltWithArguments(x.Text)
					.Where(containerBuilt.Contains)
					.Distinct()
					.Select(type => $"{x.FilePath}: {type}"))
		];

		// Assert
		containerBuilt
			.Should()
			.NotBeEmpty();

		builtWithNew
			.Should()
			.HaveCount(0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Enumerates the assemblies of the application: its own assembly and the assemblies of its projects it references.
	/// </summary>
	private static Assembly[] EnumerateAppAssemblies()
	{
		string prefix = typeof(App).Namespace!;

		Dictionary<string, Assembly> found = [];

		Queue<Assembly> pending = new([typeof(App).Assembly]);

		while (pending.TryDequeue(out Assembly? assembly))
		{
			if (!found.TryAdd(assembly.GetName().Name!, assembly))
			{
				continue;
			}

			foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
			{
				if (reference.Name!.StartsWith(prefix, StringComparison.Ordinal))
				{
					pending.Enqueue(Assembly.Load(reference));
				}
			}
		}

		return [.. found.Values];
	}

	/// <summary>
	/// Finds the names of the classes that a code builds with <c>new</c> and arguments.
	/// </summary>
	private static IEnumerable<string> FindBuiltWithArguments(string text)
	{
		string code = CodeText.WithoutCommentsAndLiterals(text);

		return NewWithArgumentsRegex()
			.Matches(code)
			.Concat(TargetTypedNewWithArgumentsRegex().Matches(code))
			.Select(static x => x.Groups["type"].Value);
	}

	/// <summary>
	/// Finds the names of the classes the container of the application builds: a constructor takes a service, the
	/// application never builds the class with <c>new</c>, and the class is neither a record nor an object of the
	/// interface.
	/// </summary>
	private static IEnumerable<string> FindContainerBuiltClasses()
	{
		HashSet<string> builtByApp =
		[
			.. RepositoryFiles
				.EnumerateSourceCode()
				.Select(static x => CodeText.WithoutCommentsAndLiterals(x.Text))
				.SelectMany(static code => NewRegex()
					.Matches(code)
					.Concat(TargetTypedNewRegex().Matches(code)))
				.Select(static x => x.Groups["type"].Value)
		];

		return EnumerateAppAssemblies()
			.SelectMany(static x => x.GetTypes())
			.Where(static x => x.IsClass && !x.IsAbstract && !IsRecord(x) && !typeof(AvaloniaObject).IsAssignableFrom(x))
			.Where(static x => x
				.GetConstructors()
				.Any(static constructor => constructor
					.GetParameters()
					.Any(static parameter => parameter.ParameterType.IsInterface)))
			.Select(static x => x.Name)
			.Where(name => !builtByApp.Contains(name));
	}

	/// <summary>
	/// Tells whether a class is a record: data the application builds, never a service of the container.
	/// </summary>
	private static bool IsRecord(Type type) => type.GetMethod(RecordCloneMethodName) is not null;

	/// <summary>
	/// Matches a class built with <c>new</c> and its name.
	/// </summary>
	[GeneratedRegex(@"\bnew\s+(?<type>\w+)\s*\(")]
	private static partial Regex NewRegex();

	/// <summary>
	/// Matches a class built with <c>new</c> and arguments, and its name.
	/// </summary>
	[GeneratedRegex(@"\bnew\s+(?<type>\w+)\s*\(\s*(?!\))")]
	private static partial Regex NewWithArgumentsRegex();

	/// <summary>
	/// Matches a declaration of a class built with a target-typed <c>new</c>, and the name of the class.
	/// </summary>
	[GeneratedRegex(@"\b(?<type>\w+)\s+\w+\s*=\s*new\s*\(")]
	private static partial Regex TargetTypedNewRegex();

	/// <summary>
	/// Matches a declaration of a class built with a target-typed <c>new</c> and arguments, and the name of the class.
	/// </summary>
	[GeneratedRegex(@"\b(?<type>\w+)\s+\w+\s*=\s*new\s*\(\s*(?!\))")]
	private static partial Regex TargetTypedNewWithArgumentsRegex();
	#endregion
}
