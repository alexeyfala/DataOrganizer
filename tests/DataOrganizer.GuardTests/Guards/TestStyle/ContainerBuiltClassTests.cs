using Avalonia;
using AwesomeAssertions;
using DataOrganizer.GuardTests.TestHelpers;
using DataOrganizer.TestSupport.Guards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DataOrganizer.GuardTests.Guards.TestStyle;

[Guard]
[TestFixture(Description = "Guards the tests against building with new a class that the container of the application builds")]
internal sealed partial class ContainerBuiltClassTests
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
	/// compiled application never calls a constructor of the class with <c>new</c>, and the class is neither a record
	/// nor an object of the interface.
	/// </summary>
	private static IEnumerable<string> FindContainerBuiltClasses()
	{
		Assembly[] assemblies = AppAssemblies.Enumerate();

		HashSet<Type> builtByApp = [.. assemblies.SelectMany(ConstructorCallReader.ReadBuiltTypes)];

		return assemblies
			.SelectMany(static x => x.GetTypes())
			.Where(IsContainerCandidate)
			.Where(type => !builtByApp.Contains(type))
			.Select(ReadSourceName);
	}

	/// <summary>
	/// Tells whether the container may build a class: a concrete class, neither a record nor an object of the interface,
	/// whose constructor takes a service.
	/// </summary>
	private static bool IsContainerCandidate(Type type)
	{
		return type.IsClass
			&& !type.IsAbstract
			&& !IsRecord(type)
			&& !typeof(AvaloniaObject).IsAssignableFrom(type)
			&& type
				.GetConstructors()
				.Any(static constructor => constructor
					.GetParameters()
					.Any(static parameter => parameter.ParameterType.IsInterface));
	}

	/// <summary>
	/// Tells whether a class is a record: data the application builds, never a service of the container.
	/// </summary>
	private static bool IsRecord(Type type) => type.GetMethod(RecordCloneMethodName) is not null;

	/// <summary>
	/// Matches a class, generic or not, built with <c>new</c> and arguments, and its name.
	/// </summary>
	[GeneratedRegex(@"\bnew\s+(?<type>\w+)\s*(?:<[^()]*?>)?\s*\(\s*(?!\))")]
	private static partial Regex NewWithArgumentsRegex();

	/// <summary>
	/// Reads the name of a class as a source writes it, without the number of its type parameters.
	/// </summary>
	private static string ReadSourceName(Type type) => type.Name.Split('`')[0];

	/// <summary>
	/// Matches a declaration of a class, generic or not, built with a target-typed <c>new</c> and arguments, and the name
	/// of the class.
	/// </summary>
	[GeneratedRegex(@"\b(?<type>\w+)(?:<[^()=;]*?>)?\s+\w+\s*=\s*new\s*\(\s*(?!\))")]
	private static partial Regex TargetTypedNewWithArgumentsRegex();
	#endregion
}
