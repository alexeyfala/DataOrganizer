using DataOrganizer.Extensions;
using Serilog.Core;
using Serilog.Events;
using Shared.Common;
using System;
using System.Diagnostics;

namespace DataOrganizer.Helpers;

/// <summary>
/// Provides the ability to programmatically read logs.
/// </summary>
internal sealed class LogCallbackSink : ILogEventSink
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
		string message = $"[{logEvent.Timestamp.ToString(AppUtils.LogTimestampFormat)}] {logEvent.Level.ToShort()} {logEvent.RenderMessage()}{Environment.NewLine}";

		if (logEvent.Exception is { } ex)
		{
			message += ex.ToStringDemystified() + Environment.NewLine;
		}

		LogCallback(message.Replace(@"\""", @""""));
	}
	#endregion
}
