using DataOrganizer.Infrastructure.Diagnostics.Extensions;
using DataOrganizer.Shared.Common;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Diagnostics;

namespace DataOrganizer.Infrastructure.Diagnostics.Services;

/// <summary>
/// Provides the ability to programmatically read logs.
/// </summary>
public sealed class LogCallbackSink : ILogEventSink
{
	#region Properties
	/// <summary>
	/// Reference to the method for receiving messages.
	/// </summary>
	public required Action<string> LogCallback { get; init; }
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Emit(LogEvent logEvent)
	{
		string message = $"[{logEvent.Timestamp.ToString(LogDefaults.TimestampFormat)}] {logEvent.Level.ToShort()} {logEvent.RenderMessage()}{Environment.NewLine}";

		if (logEvent.Exception is { } ex)
		{
			message += ex.ToStringDemystified() + Environment.NewLine;
		}

		LogCallback(message.Replace(@"\""", @""""));
	}
	#endregion
}
