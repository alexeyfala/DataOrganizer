using AwesomeAssertions;
using DataOrganizer.Shared.Extensions;
using System.Reflection;

namespace DataOrganizer.Shared.UnitTests.Extensions;

/// <summary>
/// Tests of <see cref="Shared.Extensions.AssemblyExtensions" /> type.
/// </summary>
[TestFixture]
internal sealed class AssemblyExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="Shared.Extensions.AssemblyExtensions.GetVersionWithSuffix" />: the version is the informational one, suffix included.
	/// </summary>
	[Test]
	public void GetVersionWithSuffix_Reads_The_Informational_Version()
	{
		// Arrange
		Assembly assembly = typeof(Shared.Extensions.AssemblyExtensions).Assembly;

		// Act
		string? version = assembly.GetVersionWithSuffix();

		// Assert
		version
			.Should()
			.Be(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion);
	}

	/// <summary>
	/// <see cref="Shared.Extensions.AssemblyExtensions.GetVersionWithSuffix" />: no assembly has no version.
	/// </summary>
	[Test]
	public void GetVersionWithSuffix_Returns_Null_Without_An_Assembly()
	{
		// Arrange
		Assembly? assembly = null;

		// Act
		string? version = assembly.GetVersionWithSuffix();

		// Assert
		version
			.Should()
			.BeNull();
	}
	#endregion
}
