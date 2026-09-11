using System.Diagnostics;

namespace Shared.Extensions;

public static class StopwatchExtensions
{
	#region Methods
	/// <summary>
	/// Returns the elapsed time as a string.
	/// </summary>
	public static string GetElapsedTime(this Stopwatch stopwatch, bool includeHours = true)
	{
		return includeHours
			? $@"{stopwatch.Elapsed:hh\:mm\:ss}.{stopwatch.Elapsed.Milliseconds.TakeDigits(3):000}"
			: $@"{stopwatch.Elapsed:mm\:ss}.{stopwatch.Elapsed.Milliseconds.TakeDigits(3):000}";
	}
	#endregion
}
