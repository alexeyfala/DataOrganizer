using System.Diagnostics;

namespace Shared.Extensions;

public static class StopwatchExtensions
{
	#region Methods
	/// <summary>
	/// Returns the elapsed time as a string.
	/// </summary>
	public static string GetElapsedTime(this Stopwatch timer, bool includeHours = true)
	{
		return includeHours
			? $@"{timer.Elapsed:hh\:mm\:ss}.{timer.Elapsed.Milliseconds.TakeDigits(3):000}"
			: $@"{timer.Elapsed:mm\:ss}.{timer.Elapsed.Milliseconds.TakeDigits(3):000}";
	}
	#endregion
}
