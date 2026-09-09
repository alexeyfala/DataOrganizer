using Avalonia;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Repository.Services;
using Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(App)}"" type")]
internal class AppTests
{
	#region Data
	/// <summary>
	/// Assemblies whose types the application registers itself.
	/// </summary>
	private static readonly Assembly[] _ownAssemblies =
	[
		typeof(App).Assembly,
		typeof(DbAccess).Assembly,
		typeof(FileSystem).Assembly
	];
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="App.RegisterServices" />: every registered type can be built from the registrations
	/// alone, so a missing one is named here instead of failing on the first resolve at startup.
	/// </summary>
	[AvaloniaTest]
	public void RegisterServices_Registers_Every_Dependency()
	{
		// Arrange
		ServiceCollection services = [];

		((App)Application.Current!).RegisterServices(services, []);

		// Act
		string[] unsatisfied =
		[
			.. services
				.Select(GetImplementationType)
				.Where(IsOwnType)
				.Distinct()
				.Select(x => DescribeMissingDependencies(services, x!))
				.Where(x => x.Length > 0)
		];

		// Assert
		unsatisfied
			.Should()
			.BeEmpty();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when a parameter can be obtained from the registrations.
	/// </summary>
	private static bool CanResolve(ServiceCollection services, ParameterInfo parameter)
	{
		if (parameter.HasDefaultValue || parameter.ParameterType == typeof(IServiceProvider))
		{
			return true;
		}

		Type type = parameter.ParameterType;

		if (parameter.GetCustomAttribute<FromKeyedServicesAttribute>() is { } keyed)
		{
			return services.Any(x => x.IsKeyedService
				&& x.ServiceType == type
				&& Equals(x.ServiceKey, keyed.Key));
		}

		if (services.Any(x => !x.IsKeyedService && x.ServiceType == type))
		{
			return true;
		}

		if (!type.IsGenericType)
		{
			return false;
		}

		Type definition = type.GetGenericTypeDefinition();

		// A sequence is satisfied by any number of registrations, an open generic by a closed request.
		return definition == typeof(IEnumerable<>)
			|| services.Any(x => !x.IsKeyedService && x.ServiceType == definition);
	}

	/// <summary>
	/// Names the dependencies no constructor of a type can obtain; empty when the type can be built.
	/// </summary>
	private static string DescribeMissingDependencies(ServiceCollection services, Type implementationType)
	{
		ConstructorInfo[] constructors = implementationType.GetConstructors();

		if (constructors.Length == 0)
		{
			return string.Empty;
		}

		List<string> missingPerConstructor = [];

		foreach (ConstructorInfo constructor in constructors)
		{
			string[] missing =
			[
				.. constructor
					.GetParameters()
					.Where(x => !CanResolve(services, x))
					.Select(x => x.ParameterType.Name)
			];

			if (missing.Length == 0)
			{
				return string.Empty;
			}

			missingPerConstructor.Add(string.Join(", ", missing));
		}

		return $"{implementationType.Name} needs {string.Join(" | ", missingPerConstructor)}";
	}

	/// <summary>
	/// Type a registration builds, or <c>null</c> when it hands out an instance or a factory.
	/// </summary>
	private static Type? GetImplementationType(ServiceDescriptor descriptor)
	{
		return descriptor.IsKeyedService
			? descriptor.KeyedImplementationType
			: descriptor.ImplementationType;
	}

	/// <summary>
	/// <c>True</c> for a closed type of the application itself.
	/// </summary>
	private static bool IsOwnType(Type? type)
	{
		return type is { ContainsGenericParameters: false }
			&& _ownAssemblies.Contains(type.Assembly);
	}
	#endregion
}
