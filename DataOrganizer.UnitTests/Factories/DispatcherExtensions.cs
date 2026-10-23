using Avalonia.Threading;
using Shared.Extensions;
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
	public static ValueTask<bool> RunJobsUntilAsync(
		this Dispatcher dispatcher,
		Func<bool> condition,
		int millisecondsDelay = 10,
		int maxRepeats = 300)
	{
		Func<bool> isMet = () =>
		{
			dispatcher.RunJobs();

			return condition();
		};

		return isMet.WaitAsync(millisecondsDelay, maxRepeats);
	}
	#endregion
}
