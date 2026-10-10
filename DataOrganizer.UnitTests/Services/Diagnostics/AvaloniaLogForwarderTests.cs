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
	/// Start of the entry that Avalonia writes when a call to the input context of a popup or a window fails.
	/// </summary>
	private const string CallFailure = "Error:\n";

	/// <summary>
	/// Start of the entry that Avalonia writes when it fails to destroy the input context of a popup or a window.
	/// </summary>
	private const string DestroyFailure = "Error while destroying the context:\n";

	/// <summary>
	/// Exception with which ibus-portal answers a call to an input context that a closing popup or window destroyed.
	/// </summary>
	private const string GoneContextReply =
		"Tmds.DBus.Protocol.DBusErrorReplyException: org.freedesktop.DBus.Error.UnknownMethod: "
		+ "Object does not exist at path /org/freedesktop/IBus/InputContext_9";

	/// <summary>
	/// Exception with which ibus-portal answers the destruction of an input context, although it destroys it.
	/// </summary>
	private const string IBusDestroyReply =
		"Tmds.DBus.Protocol.DBusErrorReplyException: org.freedesktop.DBus.Error.UnknownMethod: "
		+ "Method Destroy is not implemented on interface org.freedesktop.IBus.Service";

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
	/// <see cref="AvaloniaLogForwarder.IsIBusClosingNoise" />: verifies only the errors of IBus about the input context of
	/// a closing popup or window are detected, in any language of the system.
	/// </summary>
	[TestCase("IME", DestroyFailure + IBusDestroyReply, ExpectedResult = true)]
	[TestCase("IME", CallFailure + GoneContextReply, ExpectedResult = true)]
	[TestCase("IME", DestroyFailure + GoneContextReply, ExpectedResult = true)]
	[TestCase("IME", CallFailure + "Tmds.DBus.Protocol.DBusErrorReplyException: org.freedesktop.DBus.Error.UnknownMethod: "
		+ "Objekt existiert nicht unter Pfad /org/freedesktop/IBus/InputContext_9", ExpectedResult = true)]
	[TestCase(LogArea.FreeDesktopPlatform, DestroyFailure + IBusDestroyReply, ExpectedResult = false)]
	[TestCase("IME", "Unable to create IME input context:\n" + GoneContextReply, ExpectedResult = false)]
	[TestCase("IME", DestroyFailure + "Tmds.DBus.Protocol.DisconnectedException: Connection closed by peer.",
		ExpectedResult = false)]
	[TestCase("IME", CallFailure + "Tmds.DBus.Protocol.DBusErrorReplyException: org.freedesktop.DBus.Error.UnknownMethod: "
		+ "No such method SetCapabilities", ExpectedResult = false)]
	[TestCase("IME", CallFailure + "Tmds.DBus.Protocol.DBusErrorReplyException: org.freedesktop.DBus.Error.AccessDenied: "
		+ "Rejected send message to /org/freedesktop/IBus/InputContext_9", ExpectedResult = false)]
	public bool IsIBusClosingNoise_Detects_The_IBus_Errors_Of_A_Closing_Window(string area, string message)
	{
		// Act
		return AvaloniaLogForwarder.IsIBusClosingNoise(area, message);
	}

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
	/// <see cref="AvaloniaLogForwarder.Log(LogEventLevel, string, object, string, object[])" />: a value that may come
	/// from the data reaches the log of the application only as its length.
	/// </summary>
	[TestCase("Codepoint")]
	[TestCase("Message")]
	[TestCase("Uri")]
	[TestCase("$Value")]
	public void Log_Hides_A_Value_That_May_Come_From_The_Data(string hole)
	{
		// Arrange
		const string value = "secret";

		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(logger));

		AvaloniaLogForwarder sut = mock.Create<AvaloniaLogForwarder>();

		// Act
		sut.Log(
			LogEventLevel.Warning,
			LogArea.Platform,
			null,
			$"Failed for {{{hole}}}.",
			value);

		// Assert
		logger.Received(1).Warning(
			Arg.Any<string>(),
			Arg.Is("[Platform] Failed for (6 characters)."),
			Arg.Any<string>());
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
	/// <see cref="AvaloniaLogForwarder.Log(LogEventLevel, string, object, string, object[])" />: the exception of a
	/// technical failure reaches the log of the application as it is.
	/// </summary>
	[Test]
	public void Log_Shows_The_Exception_Of_A_Failure()
	{
		// Arrange
		const string reason = "The name com.canonical.AppMenu.Registrar was not provided";

		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(logger));

		AvaloniaLogForwarder sut = mock.Create<AvaloniaLogForwarder>();

		// Act
		sut.Log(
			LogEventLevel.Warning,
			LogArea.Platform,
			null,
			"DBusMenu UnregisterWindowAsync failed: {Exception}",
			new InvalidOperationException(reason));

		// Assert
		logger.Received(1).Warning(
			Arg.Any<string>(),
			Arg.Is<string>(x => x.Contains(reason)),
			Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="AvaloniaLogForwarder.Log(LogEventLevel, string, object, string)" />: the noise of IBus stays out of the
	/// log of the application on Linux, the only system that brings it, and reaches the log elsewhere.
	/// </summary>
	[Test]
	public void Log_Skips_The_IBus_Noise_Only_On_Linux()
	{
		// Arrange
		int writtenEntries = OperatingSystem.IsLinux() ? 0 : 1;

		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(logger));

		AvaloniaLogForwarder sut = mock.Create<AvaloniaLogForwarder>();

		// Act
		sut.Log(
			LogEventLevel.Error,
			"IME",
			null,
			DestroyFailure + IBusDestroyReply);

		// Assert
		logger.Received(writtenEntries).Error(
			Arg.Any<string>(),
			Arg.Any<string>(),
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
