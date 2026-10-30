using AwesomeAssertions;
using Diagnostics.Services;
using Serilog.Events;
using Serilog.Parsing;
using System;

namespace Diagnostics.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(LogCallbackSink)}"" type")]
internal class LogCallbackSinkTests
{
	#region Methods
	/// <summary>
	/// <see cref="LogCallbackSink.Emit" />: the exception of an event follows its line, on lines of its own.
	/// </summary>
	[Test]
	public void Emit_Adds_The_Exception_After_The_Line()
	{
		// Arrange
		string? received = null;

		LogCallbackSink sut = new()
		{
			LogCallback = x => received = x
		};

		InvalidOperationException exception = new("failure");

		// Act
		sut.Emit(CreateEvent("message", exception));

		// Assert
		received
			.Should()
			.Contain($"message{Environment.NewLine}")
			.And
			.Contain(exception.Message)
			.And
			.EndWith(Environment.NewLine);
	}

	/// <summary>
	/// <see cref="LogCallbackSink.Emit" />: the line carries the short level and the rendered message and ends with a line
	/// break.
	/// </summary>
	[Test]
	public void Emit_Hands_The_Line_With_Its_Level_To_The_Callback()
	{
		// Arrange
		string? received = null;

		LogCallbackSink sut = new()
		{
			LogCallback = x => received = x
		};

		// Act
		sut.Emit(CreateEvent("message", exception: null));

		// Assert
		received
			.Should()
			.Contain("[INF] message")
			.And
			.EndWith(Environment.NewLine);
	}

	/// <summary>
	/// <see cref="LogCallbackSink.Emit" />: the quotes that the rendering escapes are shown plain.
	/// </summary>
	[Test]
	public void Emit_Shows_The_Escaped_Quotes_Plain()
	{
		// Arrange
		string? received = null;

		LogCallbackSink sut = new()
		{
			LogCallback = x => received = x
		};

		// Act
		sut.Emit(CreateEvent("say \\\"hi\\\"", exception: null));

		// Assert
		received
			.Should()
			.Contain("say \"hi\"");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates an event of the information level whose message is the given text.
	/// </summary>
	private static LogEvent CreateEvent(string text, Exception? exception)
	{
		return new LogEvent(
			DateTimeOffset.Now,
			LogEventLevel.Information,
			exception,
			new MessageTemplateParser().Parse(text),
			[]);
	}
	#endregion
}
