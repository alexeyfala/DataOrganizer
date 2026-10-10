using Avalonia;
using Avalonia.Logging;
using DataOrganizer.Interfaces.Diagnostics;
using Serilog;
using Shared.Extensions;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.Services.Diagnostics;

internal sealed partial class AvaloniaLogForwarder : IAvaloniaLogForwarder, ILogSink
{
	#region Data
	/// <summary>
	/// Lowest level of the entries written to <see cref="ILogger" />: the levels below it carry the values of
	/// properties.
	/// </summary>
	private const LogEventLevel MinimumLevel = LogEventLevel.Warning;

	/// <summary>
	/// Names of the holes of a message template whose values come from code or markup, never from the data.
	/// </summary>
	private static readonly string[] PlainHoles = ["Expression", "ExpressionErrorPoint", "Property"];

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <summary>
	/// Sink of the log of Avalonia that was in place before this one, or <c>null</c> when there was none.
	/// </summary>
	private ILogSink? _previous;
	#endregion

	#region Constructors
	public AvaloniaLogForwarder(ILogger logger) => _logger = logger;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose()
	{
		if (Logger.Sink != this)
		{
			return;
		}

		Logger.Sink = _previous;
	}

	/// <inheritdoc />
	public bool IsEnabled(LogEventLevel level, string area)
	{
		return level >= MinimumLevel || _previous?.IsEnabled(level, area) == true;
	}

	/// <inheritdoc />
	public void Log(
		LogEventLevel level,
		string area,
		object? source,
		string messageTemplate)
	{
		_previous?.Log(
			level,
			area,
			source,
			messageTemplate);

		Write(
			level,
			area,
			source,
			messageTemplate,
			[]);
	}

	/// <inheritdoc />
	public void Log(
		LogEventLevel level,
		string area,
		object? source,
		string messageTemplate,
		params object?[] propertyValues)
	{
		_previous?.Log(
			level,
			area,
			source,
			messageTemplate,
			propertyValues);

		Write(
			level,
			area,
			source,
			messageTemplate,
			propertyValues);
	}

	/// <inheritdoc />
	public void StartForwarding()
	{
		_previous = Logger.Sink;

		Logger.Sink = this;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the type and the name of the element an entry comes from.
	/// </summary>
	private static string DescribeSource(object? source)
	{
		return source switch
		{
			null => string.Empty,
			StyledElement { Name: { } name } => $" ({source.GetType().Name} #{name})",
			_ => $" ({source.GetType().Name})"
		};
	}

	/// <summary>
	/// Returns the value of a hole as it is when it comes from code or markup, and otherwise only the length of its
	/// text.
	/// </summary>
	private static string DescribeValue(string hole, object? value)
	{
		if (value?.ToString() is not { } text)
		{
			return "null";
		}

		return PlainHoles.Contains(hole) ? $"'{text}'" : $"({text.Length} characters)";
	}

	/// <summary>
	/// Matches a hole of a message template, or a doubled brace that stands for a single one.
	/// </summary>
	[GeneratedRegex(@"\{\{|\}\}|\{[$@]?(?<name>[^{}:,]*)[^{}]*\}")]
	private static partial Regex HoleRegex();

	/// <summary>
	/// Returns the text of an entry, with the values that may come from the data reduced to their length.
	/// </summary>
	private static string Render(
		string area,
		object? source,
		string messageTemplate,
		object?[] propertyValues)
	{
		int index = 0;

		string message = HoleRegex().Replace(messageTemplate, x => x.Value switch
		{
			"{{" => "{",
			"}}" => "}",
			_ => DescribeValue(x.Groups["name"].Value, propertyValues.ElementAtOrDefault(index++))
		});

		return $"[{area}] {message}{DescribeSource(source)}";
	}

	/// <summary>
	/// Writes an entry to <see cref="ILogger" /> when its level is high enough, with its source at the end, as every
	/// entry of the application.
	/// </summary>
	private void Write(
		LogEventLevel level,
		string area,
		object? source,
		string messageTemplate,
		object?[] propertyValues)
	{
		if (level < MinimumLevel)
		{
			return;
		}

		string text = Render(
			area,
			source,
			messageTemplate,
			propertyValues);

		if (level == LogEventLevel.Warning)
		{
			_logger.LogWarning(text);

			return;
		}

		// A fatal entry goes as an error, since there is no method for it; the errors of Avalonia do not stop the debugger.
		_logger.LogError(text, breakInDebugger: false);
	}
	#endregion
}
