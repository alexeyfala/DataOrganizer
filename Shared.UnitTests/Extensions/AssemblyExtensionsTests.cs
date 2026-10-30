using AwesomeAssertions;
using Shared.Extensions;
using System.Reflection;

namespace Shared.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(Shared.Extensions.AssemblyExtensions)}"" type")]
internal class AssemblyExtensionsTests
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
