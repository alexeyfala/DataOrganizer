using AwesomeAssertions;
using DataOrganizer.Extensions;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(CommandLineOptionsExtensions)}"" type")]
internal class CommandLineOptionsExtensionsTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="CommandLineOptionsExtensions.AddConsoleArg" />: appends the "--console" argument.
	/// </summary>
	[Test]
	public void AddConsoleArg_Appends_Console_Argument()
	{
		// Arrange
		string[] args = ["existing"];

		// Act
		string[] result = args.AddConsoleArg();

		// Assert
		result
			.Should()
			.HaveCount(2);

		result[^1]
			.Should()
			.Be("--console");
	}

	/// <summary>
	/// Test of <see cref="CommandLineOptionsExtensions.AddConsoleArg" />: returns a new array without mutating the original.
	/// </summary>
	[Test]
	public void AddConsoleArg_Does_Not_Mutate_Original_Array()
	{
		// Arrange
		string[] args = ["a", "b"];

		// Act
		string[] result = args.AddConsoleArg();

		// Assert
		args
			.Should()
			.HaveCount(2);

		result
			.Should()
			.NotBeSameAs(args);
	}

	/// <summary>
	/// Test of <see cref="CommandLineOptionsExtensions.AddDebugArg" />: appends the "--debug" argument.
	/// </summary>
	[Test]
	public void AddDebugArg_Appends_Debug_Argument()
	{
		// Arrange
		string[] args = [];

		// Act
		string[] result = args.AddDebugArg();

		// Assert
		result
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be("--debug");
	}

	/// <summary>
	/// Test of <see cref="CommandLineOptionsExtensions.AddFillObjectsArg" />: appends the "--fillobjects" argument.
	/// </summary>
	[Test]
	public void AddFillObjectsArg_Appends_FillObjects_Argument()
	{
		// Arrange
		string[] args = [];

		// Act
		string[] result = args.AddFillObjectsArg();

		// Assert
		result
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be("--fillobjects");
	}

	/// <summary>
	/// Test of <see cref="CommandLineOptionsExtensions.AddHelpArg" />: appends the "--help" argument.
	/// </summary>
	[Test]
	public void AddHelpArg_Appends_Help_Argument()
	{
		// Arrange
		string[] args = [];

		// Act
		string[] result = args.AddHelpArg();

		// Assert
		result
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be("--help");
	}
	#endregion
}
