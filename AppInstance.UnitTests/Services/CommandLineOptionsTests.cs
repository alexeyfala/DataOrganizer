using AppInstance.Extensions;
using AppInstance.Services;
using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Serilog.Events;
using System;
using System.Linq;

namespace AppInstance.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(CommandLineOptions)}"" type")]
internal class CommandLineOptionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="CommandLineOptions" />: the constructor initializes properties from the parsed command-line arguments.
	/// </summary>
	[Test]
	public void Constructor_Initializes_Properties_From_Arguments()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		string[] args = Array.Empty<string>()
			.AddConsoleArg()
			.AddDebugArg()
			.AddHelpArg();

		// Act
		CommandLineOptions sut = mock.Create<CommandLineOptions>(TypedParameter.From(args));

		// Assert
		sut.ShowConsole
			.Should()
			.BeTrue();

		sut.MinimumLogEventLevel
			.Should()
			.Be(LogEventLevel.Debug);

		sut.PrintHelp
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="CommandLineOptions(string[])" />: without arguments the log takes information, and neither the console
	/// nor the help is asked for.
	/// </summary>
	[Test]
	public void Constructor_Keeps_The_Defaults_Without_Arguments()
	{
		// Act
		CommandLineOptions sut = new([]);

		// Assert
		sut.ShowConsole
			.Should()
			.BeFalse();

		sut.MinimumLogEventLevel
			.Should()
			.Be(LogEventLevel.Information);

		sut.PrintHelp
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="CommandLineOptions.GetHelp" />: a heading and then each argument in its order on a line of its own, with
	/// a description of its own that starts in one column for all, and no line break after the last.
	/// </summary>
	[Test]
	public void GetHelp_Lists_Each_Argument_With_Its_Description_In_One_Column()
	{
		// Arrange
		CommandLineOptions sut = new([]);

		// Act
		string help = sut.GetHelp();

		// Assert
		string[] lines = help.Split(Environment.NewLine);

		string[] arguments = lines[1..];

		lines[0]
			.Should()
			.Be("Available command line arguments:");

		arguments.Select(static x => x[..x.IndexOf(':')])
			.Should()
			.Equal("--console", "--debug", "--help");

		arguments.Select(static x => x.Length - x[(x.IndexOf(':') + 1)..].TrimStart().Length)
			.Should()
			.AllBeEquivalentTo(arguments[0].IndexOf(':') + 2);

		arguments.Select(static x => x[(x.IndexOf(':') + 1)..].Trim())
			.Should()
			.OnlyHaveUniqueItems()
			.And
			.NotContain(string.Empty);
	}
	#endregion
}
