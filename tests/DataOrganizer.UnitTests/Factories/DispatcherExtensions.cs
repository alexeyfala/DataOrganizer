using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Factories;

/// <summary>
/// Waits of headless tests for work that reaches the UI thread on real time.
/// </summary>
internal static class DispatcherExtensions
{
	#region Methods
	/// <summary>
	/// Runs the jobs of <paramref name="dispatcher" /> until <paramref name="condition" /> holds, pausing
	/// <paramref name="millisecondsDelay" /> between the runs; <c>false</c> after <paramref name="maxRepeats" /> pauses.
	/// </summary>
	public static async ValueTask<bool> RunJobsUntilAsync(
		this Dispatcher dispatcher,
		Func<bool> condition,
		int millisecondsDelay = 10,
		int maxRepeats = 300)
	{
		for (int i = 0; ; i++)
		{
			dispatcher.RunJobs();

			if (condition())
			{
				return true;
			}

			if (i == maxRepeats)
			{
				return false;
			}

			// A plain await comes back to the UI thread, where the jobs run.
			await Task.Delay(millisecondsDelay);
		}
	}
	#endregion
}
