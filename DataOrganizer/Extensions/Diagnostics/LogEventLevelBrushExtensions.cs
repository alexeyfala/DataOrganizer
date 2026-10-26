using Avalonia.Media;
using Serilog.Events;

namespace DataOrganizer.Extensions.Diagnostics;

internal static class LogEventLevelBrushExtensions
{
	#region Methods
	/// <summary>
	/// Returns the brush color depending on <see cref="LogEventLevel" />.
	/// </summary>
	public static IImmutableSolidColorBrush ToBrush(this LogEventLevel level) => level switch
	{
		LogEventLevel.Verbose => Brushes.Gray,
		LogEventLevel.Debug => Brushes.CadetBlue,
		LogEventLevel.Information => Brushes.LimeGreen,
		LogEventLevel.Warning => Brushes.Orange,
		LogEventLevel.Error or LogEventLevel.Fatal => Brushes.Red,
		_ => Brushes.Transparent
	};
	#endregion
}
