using Avalonia.Media;
using Serilog.Events;

namespace DataOrganizer.Extensions;

internal static class LogEventLevelExtensions
{
	#region Methods
	/// <summary>
	/// Returns the brush color depending on <see cref="LogEventLevel" />.
	/// </summary>
	public static IImmutableSolidColorBrush ToBrush(this LogEventLevel level) => level switch
	{
		LogEventLevel.Debug => Brushes.CadetBlue,
		LogEventLevel.Information => Brushes.LimeGreen,
		LogEventLevel.Warning => Brushes.Orange,
		LogEventLevel.Error or LogEventLevel.Fatal => Brushes.Red,
		_ => Brushes.Transparent
	};

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
