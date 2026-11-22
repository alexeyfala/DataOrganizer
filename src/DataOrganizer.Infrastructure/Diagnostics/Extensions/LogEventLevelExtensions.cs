using Serilog.Events;

namespace DataOrganizer.Infrastructure.Diagnostics.Extensions;

/// <summary>
/// Names the levels of the log by their short tags.
/// </summary>
public static class LogEventLevelExtensions
{
	#region Methods
	/// <summary>
	/// Returns a shortened text value depending on <see cref="LogEventLevel" />.
	/// </summary>
	public static string ToShort(this LogEventLevel level) => level switch
	{
		LogEventLevel.Verbose => "[VERBOSE]",
		LogEventLevel.Debug => "[DBG]",
		LogEventLevel.Information => "[INF]",
		LogEventLevel.Warning => "[WRN]",
		LogEventLevel.Error => "[ERR]",
		LogEventLevel.Fatal => "[FTL]",
		_ => string.Empty,
	};
	#endregion
}
