using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Logging;
using AwesomeAssertions;
using DataOrganizer.Services.Diagnostics;
using NSubstitute;
using Serilog;
using System;

namespace DataOrganizer.UnitTests.Services.Diagnostics;

[TestFixture(Description = $@"Tests of ""{nameof(AvaloniaLogForwarder)}"" type")]
internal class AvaloniaLogForwarderTests
{
	#region Data
	/// <summary>
	/// Template of the entry that Avalonia writes for each value set on a property.
	/// </summary>
	private const string PropertySetTemplate = "Set {Property} to {$Value} with priority {Priority}";

	/// <summary>
	/// Sink of the log of Avalonia as it was before a test replaced it.
	/// </summary>
	private ILogSink? _saved;
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="AvaloniaLogForwarder.Log(LogEventLevel, string, object, string, object[])" />: the sink that was in
	/// place before gets every entry as it is, those below the warnings too.
	/// </summary>
	[Test]
	public void Log_Hands_Every_Entry_To_The_Replaced_Sink()
	{
		// Arrange
		ILogSink replaced = Substitute.For<ILogSink>();

		object?[] values = ["Text", "secret", "LocalValue"];

		using AutoMock mock = AutoMock.GetLoose();

		AvaloniaLogForwarder sut = mock.Create<AvaloniaLogForwarder>();

		Logger.Sink = replaced;

		sut.StartForwarding();

		// Act
		sut.Log(
			LogEventLevel.Verbose,
			LogArea.Property,
			null,
			PropertySetTemplate,
			values);

		// Assert
		replaced.Received(1).Log(
			LogEventLevel.Verbose,
			LogArea.Property,
			null,
			PropertySetTemplate,
			values);
	}

	/// <summary>
	/// <see cref="AvaloniaLogForwarder.Log(LogEventLevel, string, object, string, object[])" />: the value that
	/// a binding failed to convert does not reach the log of the application.
	/// </summary>
	[Test]
	public void Log_Hides_The_Value_Of_A_Failed_Conversion()
	{
		// Arrange
		const string value = "secret";

		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(logger));

		AvaloniaLogForwarder sut = mock.Create<AvaloniaLogForwarder>();

		// Act
		sut.Log(
			LogEventLevel.Warning,
			LogArea.Binding,
			null,
			"An error occurred binding {Property} to {Expression} at {ExpressionErrorPoint}: {Message}",
			"Text",
			"Value",
			"Value",
			$"Could not convert '{value}' (System.String) to 'System.Int32'.");

		// Assert
		logger.Received(1).Warning(
			Arg.Any<string>(),
			Arg.Is<string>(x => !x.Contains(value)),
			Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="AvaloniaLogForwarder.Log(LogEventLevel, string, object, string)" />: a warning ends with the forwarder
	/// as its source, as every entry of the application ends with its own.
	/// </summary>
	[Test]
	public void Log_Writes_A_Warning_With_Its_Source()
	{
		// Arrange
		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(logger));

		AvaloniaLogForwarder sut = mock.Create<AvaloniaLogForwarder>();

		// Act
		sut.Log(
			LogEventLevel.Warning,
			LogArea.Layout,
			null,
			"Layout cycle detected.");

		// Assert
		logger.Received(1).Warning(
			Arg.Any<string>(),
			Arg.Is("[Layout] Layout cycle detected."),
			Arg.Is<string>(x => x.EndsWith($"{nameof(AvaloniaLogForwarder)}.cs", StringComparison.Ordinal)));
	}

	/// <summary>
	/// <see cref="AvaloniaLogForwarder.Log(LogEventLevel, string, object, string)" />: an error, and a fatal entry as an
	/// error, ends with the forwarder as its source.
	/// </summary>
	[TestCase(LogEventLevel.Error)]
	[TestCase(LogEventLevel.Fatal)]
	public void Log_Writes_An_Error_With_Its_Source(LogEventLevel level)
	{
		// Arrange
		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(logger));

		AvaloniaLogForwarder sut = mock.Create<AvaloniaLogForwarder>();

		// Act
		sut.Log(
			level,
			"IME",
			null,
			"Error while destroying the context:");

		// Assert
		logger.Received(1).Error(
			Arg.Any<string>(),
			Arg.Is("[IME] Error while destroying the context:"),
			Arg.Is<string>(x => x.EndsWith($"{nameof(AvaloniaLogForwarder)}.cs", StringComparison.Ordinal)));
	}

	/// <summary>
	/// <see cref="AvaloniaLogForwarder.Log(LogEventLevel, string, object, string, object[])" />: an entry below the
	/// warnings, such as a value set on a property, does not reach the log of the application.
	/// </summary>
	[Test]
	public void Log_Writes_Nothing_Below_Warning()
	{
		// Arrange
		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(logger));

		AvaloniaLogForwarder sut = mock.Create<AvaloniaLogForwarder>();

		// Act
		sut.Log(
			LogEventLevel.Verbose,
			LogArea.Property,
			null,
			PropertySetTemplate,
			"Text",
			"secret",
			"LocalValue");

		// Assert
		logger.ReceivedCalls()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Remembers the sink of the log of Avalonia, which a test replaces.
	/// </summary>
	[SetUp]
	public void RememberSink() => _saved = Logger.Sink;

	/// <summary>
	/// Puts the sink of the log of Avalonia back, so that a change does not reach the tests that follow.
	/// </summary>
	[TearDown]
	public void RestoreSink() => Logger.Sink = _saved;
	#endregion
}
