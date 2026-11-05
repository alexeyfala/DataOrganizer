using AwesomeAssertions;
using Shared.Enums;
using Shared.Services;
using System;

namespace Shared.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(Platform)}"" type")]
internal class PlatformTests
{
	#region Methods
	/// <summary>
	/// <see cref="Platform.CurrentOS" />: only the flag of the system it names is set, among <see cref="Platform.IsWindows" />,
	/// <see cref="Platform.IsLinux" /> and <see cref="Platform.IsMacOS" />.
	/// </summary>
	[Test]
	public void CurrentOS_Agrees_With_The_Flags()
	{
		// Arrange
		Platform sut = new();

		// Act
		OperatingSystemKind result = sut.CurrentOS;

		// Assert
		bool[] flags = [sut.IsWindows, sut.IsLinux, sut.IsMacOS];

		flags
			.Should()
			.Equal(
				result == OperatingSystemKind.Windows,
				result == OperatingSystemKind.Linux,
				result == OperatingSystemKind.MacOS);
	}

	/// <summary>
	/// <see cref="Platform.CurrentOS" />: names the system the tests run on.
	/// </summary>
	[Test]
	public void CurrentOS_Names_The_Running_System()
	{
		// Arrange
		OperatingSystemKind expected = OperatingSystem.IsWindows()
			? OperatingSystemKind.Windows
			: OperatingSystem.IsLinux()
				? OperatingSystemKind.Linux
				: OperatingSystemKind.MacOS;

		Platform sut = new();

		// Act
		OperatingSystemKind result = sut.CurrentOS;

		// Assert
		result
			.Should()
			.Be(expected);
	}
	#endregion
}
