using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Extensions;
using DataOrganizer.Services;
using Serilog.Events;
using System;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(CommandLineOptions)}"" type")]
internal class CommandLineOptionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="CommandLineOptions" />: the constructor initializes properties from the parsed command-line arguments.
	/// </summary>
	[Test]
	public void CommandLineOptions_Inializes_Properties_From_Arguments_Through_Constructor()
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
		sut.IsConsoleNeeded
			.Should()
			.BeTrue();

		sut.MinimumLogEventLevel
			.Should()
			.Be(LogEventLevel.Debug);

		sut.PrintHelp
			.Should()
			.BeTrue();
	}
	#endregion
}
