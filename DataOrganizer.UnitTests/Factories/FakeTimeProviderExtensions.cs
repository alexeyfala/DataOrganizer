using Avalonia.Threading;
using Microsoft.Extensions.Time.Testing;
using System;

namespace DataOrganizer.UnitTests.Factories;

/// <summary>
/// Steps of a fake clock in headless tests, each followed by the jobs of the UI thread.
/// </summary>
internal static class FakeTimeProviderExtensions
{
	#region Data
	/// <summary>
	/// Step of the clock: a frame, as long as a step of a smooth scroll.
	/// </summary>
	private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(16.0);

	/// <summary>
	/// Time a wait for a condition lasts at most.
	/// </summary>
	private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(5.0);
	#endregion

	#region Methods
	/// <summary>
	/// Runs the jobs of the UI thread, then moves <paramref name="time" /> on by <paramref name="span" /> a frame at a
	/// time, running the jobs after each frame.
	/// </summary>
	public static void RunJobsFor(this FakeTimeProvider time, TimeSpan span)
	{
		Dispatcher.UIThread.RunJobs();

		for (TimeSpan passed = TimeSpan.Zero; passed < span; passed += Frame)
		{
			time.Advance(Frame);

			Dispatcher.UIThread.RunJobs();
		}
	}

	/// <summary>
	/// Runs the jobs of the UI thread and moves <paramref name="time" /> on a frame at a time, running the jobs after each
	/// frame, until <paramref name="condition" /> holds or the wait runs out.
	/// </summary>
	public static void RunJobsUntil(this FakeTimeProvider time, Func<bool> condition)
	{
		Dispatcher.UIThread.RunJobs();

		for (TimeSpan passed = TimeSpan.Zero; passed < MaxWait && !condition(); passed += Frame)
		{
			time.Advance(Frame);

			Dispatcher.UIThread.RunJobs();
		}
	}
	#endregion
}
