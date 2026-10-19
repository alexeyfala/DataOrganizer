using System;
using System.Runtime.InteropServices;

namespace DataOrganizer.UnitTests;

/// <summary>
/// Asks Windows for its finest timer step for the test run, so that the short real-time waits of the tests last as long as
/// they ask instead of being rounded up to the coarse default step.
/// </summary>
[SetUpFixture]
internal sealed partial class TimerResolutionSetUp
{
	#region Data
	/// <summary>
	/// Step of the timer asked for the test run, in milliseconds.
	/// </summary>
	private const uint TimerStep = 1;

	/// <summary>
	/// Library of the multimedia timer functions of Windows.
	/// </summary>
	private const string WinmmDll = "winmm.dll";
	#endregion

	#region Methods
	/// <summary>
	/// Asks Windows for the finest timer step before the first test.
	/// </summary>
	[OneTimeSetUp]
	public void RaiseTimerResolution()
	{
		if (!OperatingSystem.IsWindows())
		{
			return;
		}

		_ = TimeBeginPeriod(TimerStep);
	}

	/// <summary>
	/// Gives the timer step back after the last test.
	/// </summary>
	[OneTimeTearDown]
	public void RestoreTimerResolution()
	{
		if (!OperatingSystem.IsWindows())
		{
			return;
		}

		_ = TimeEndPeriod(TimerStep);
	}
	#endregion

	#region Native
	/// <summary>
	/// Asks for a timer step of <paramref name="period" /> milliseconds for this process.
	/// </summary>
	[LibraryImport(WinmmDll, EntryPoint = "timeBeginPeriod")]
	private static partial uint TimeBeginPeriod(uint period);

	/// <summary>
	/// Gives back a timer step of <paramref name="period" /> milliseconds asked before.
	/// </summary>
	[LibraryImport(WinmmDll, EntryPoint = "timeEndPeriod")]
	private static partial uint TimeEndPeriod(uint period);
	#endregion
}
