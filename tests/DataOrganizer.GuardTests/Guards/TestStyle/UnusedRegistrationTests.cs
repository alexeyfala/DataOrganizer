using AwesomeAssertions;
using DataOrganizer.GuardTests.TestHelpers;
using DataOrganizer.GuardTests.TestHelpers.Dto;
using DataOrganizer.TestSupport.Guards;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.GuardTests.Guards.TestStyle;

/// <summary>
/// Guards the tests against registering services that nothing they build takes.
/// </summary>
[Guard]
[TestFixture]
internal sealed class UnusedRegistrationTests
{
	#region Data
	/// <summary>
	/// Services through which a type resolves whatever it needs: a container that builds one may use every registration.
	/// </summary>
	private static readonly FrozenSet<string> ResolvingServices = FrozenSet.Create(
		"AutoMock",
		"IComponentContext",
		"IContainer",
		"ILifetimeScope",
		"IServiceProvider");
	#endregion

	#region Methods
	/// <summary>
	/// Every registration of a test container provides a service that a type the test builds takes, directly or through
	/// the constructors of the types the container builds for it.
	/// </summary>
	[Test]
	public void Tests_Register_Only_The_Services_Their_Classes_Take()
	{
		// Arrange
		ContainerSetup[] setups =
		[
			.. RepositoryFiles
				.EnumerateTestCode()
				.SelectMany(static x => ContainerSetupReader.Read(x.FilePath, x.Text))
		];

		// Act
		string[] unused =
		[
			.. setups.SelectMany(static setup => FindUnused(setup)
				.Select(registration => $"{setup.FilePath} {setup.Method}: {registration.Text}"))
		];

		// Assert
		setups
			.Should()
			.NotBeEmpty();

		unused
			.Should()
			.HaveCount(0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Finds the registrations of a container that no type the test builds reaches through constructors. A container
	/// whose built types are unknown, or reach a service that resolves anything, is left out.
	/// </summary>
	private static IEnumerable<ContainerRegistration> FindUnused(ContainerSetup setup)
	{
		if (setup.BuiltTypes.Length == 0 || !setup.BuiltTypes.All(KnownTypes.Contains))
		{
			return [];
		}

		HashSet<string> reached = [];

		HashSet<string> visited = [];

		Queue<string> pending = new(setup.BuiltTypes);

		while (pending.TryDequeue(out string? service))
		{
			if (!reached.Add(service))
			{
				continue;
			}

			if (ResolvingServices.Contains(service))
			{
				return [];
			}

			foreach (string implementation in GetImplementations(setup, service).Where(visited.Add))
			{
				foreach (string taken in KnownTypes.GetTakenServices(implementation))
				{
					pending.Enqueue(taken);
				}
			}
		}

		return setup
			.Registrations
			.Where(x => x.Services?.Any(reached.Contains) == false);
	}

	/// <summary>
	/// Gets the types the container builds for a service: those the test registers for it, or the service itself when
	/// it is a class the container builds unregistered.
	/// </summary>
	private static IEnumerable<string> GetImplementations(ContainerSetup setup, string service)
	{
		ContainerRegistration[] providers = [.. setup.Registrations.Where(x => x.Services?.Contains(service) == true)];

		if (providers.Length > 0)
		{
			return providers
				.Select(static x => x.Implementation)
				.OfType<string>();
		}

		return KnownTypes.IsConcreteClass(service) ? [service] : [];
	}
	#endregion
}
