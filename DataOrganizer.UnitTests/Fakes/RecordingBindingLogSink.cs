using Avalonia.Logging;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.Fakes;

/// <summary>
/// Log of Avalonia that keeps the warnings and errors of the bindings.
/// </summary>
internal sealed class RecordingBindingLogSink : ILogSink
{
	#region Properties
	/// <summary>
	/// Templates of the entries written, with their values.
	/// </summary>
	public List<string> Entries { get; } = [];
	#endregion

	#region Methods
	/// <inheritdoc />
	public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning && area == LogArea.Binding;

	/// <inheritdoc />
	public void Log(
		LogEventLevel level,
		string area,
		object? source,
		string messageTemplate)
	{
		Log(level, area, source, messageTemplate, []);
	}

	/// <inheritdoc />
	public void Log(
		LogEventLevel level,
		string area,
		object? source,
		string messageTemplate,
		params object?[] propertyValues)
	{
		if (!IsEnabled(level, area))
		{
			return;
		}

		Entries.Add($"{messageTemplate} | {string.Join(" | ", propertyValues)}");
	}
	#endregion
}
