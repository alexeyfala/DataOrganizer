using AwesomeAssertions;
using DataOrganizer.GuardTests.TestHelpers;
using DataOrganizer.TestSupport.Guards;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.GuardTests.Guards.Architecture;

[Guard]
[TestFixture(Description = "Guards the code against reading the time past the time provider")]
internal partial class SystemClockTests
{
	#region Data
	/// <summary>
	/// The reads of the system clock that stay in the application, each as the file and the form it uses: the crash log
	/// is written before the services exist.
	/// </summary>
	private static readonly string[] SourceExceptions =
	[
		"DataOrganizer/App.axaml.cs reads DateTime.Now"
	];

	/// <summary>
	/// The forms of reading the system clock in the application, each under the name a failure shows. A delay or a wait
	/// counts when its statement names no time provider. A cancellation set with <c>CancelAfter</c> always counts: the
	/// call does not show whether its source runs on a time provider.
	/// </summary>
	private static readonly Dictionary<string, Regex> SourceForms = new()
	{
		["CancelAfter"] = CancelAfterRegex(),
		["DateTime.Now"] = DateTimeNowRegex(),
		["Stopwatch"] = StopwatchRegex(),
		["Task.Delay"] = TaskDelayRegex(),
		["Thread.Sleep"] = ThreadSleepRegex(),
		["WaitAsync"] = WaitAsyncRegex()
	};

	/// <summary>
	/// The waits on the system clock that stay in the tests, each as the file and the form it uses: a limit that only
	/// guards against a hang, and a poll for work that runs on a clock the tests do not drive.
	/// </summary>
	private static readonly string[] TestExceptions =
	[
		"DataOrganizer.Infrastructure.UnitTests/Diagnostics/Services/TaskExceptionHandlerTests.cs reads Task.Delay",
		"DataOrganizer.UnitTests/TestHelpers/DispatcherExtensions.cs reads Task.Delay"
	];

	/// <summary>
	/// The forms of waiting on the system clock in the tests, each under the name a failure shows. A time used as data
	/// and a timed <c>WaitAsync</c>, which only limits a hang, are no waits.
	/// </summary>
	private static readonly Dictionary<string, Regex> TestForms = new()
	{
		["CancelAfter"] = CancelAfterRegex(),
		["SpinWait"] = SpinWaitRegex(),
		["Task.Delay"] = TaskDelayRegex(),
		["Thread.Sleep"] = ThreadSleepRegex()
	};
	#endregion

	#region Methods
	/// <summary>
	/// No file of the application reads the system clock on its own, the listed exceptions left out: the time comes from
	/// the injected <see cref="System.TimeProvider" />, which a test replaces.
	/// </summary>
	[Test]
	public void Code_Reads_The_Time_Only_Through_The_Time_Provider()
	{
		// Act
		string[] reads = [.. FindSystemClockReads(RepositoryFiles.EnumerateSourceCode(), SourceForms)];

		// Assert
		reads
			.Except(SourceExceptions)
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// Every listed exception of the application still reads the system clock, so the list keeps no entry that is no
	/// longer needed.
	/// </summary>
	[Test]
	public void Every_Exception_Still_Reads_The_System_Clock()
	{
		// Act
		string[] reads = [.. FindSystemClockReads(RepositoryFiles.EnumerateSourceCode(), SourceForms)];

		// Assert
		SourceExceptions
			.Except(reads)
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// Every listed exception of the tests still waits on the system clock, so the list keeps no entry that is no longer
	/// needed.
	/// </summary>
	[Test]
	public void Every_Test_Exception_Still_Waits_On_The_System_Clock()
	{
		// Act
		string[] reads = [.. FindSystemClockReads(RepositoryFiles.EnumerateTestCode(), TestForms)];

		// Assert
		TestExceptions
			.Except(reads)
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// No test waits on the system clock, the listed exceptions left out: a test moves a fake time provider instead.
	/// </summary>
	[Test]
	public void Tests_Wait_Only_Through_The_Time_Provider()
	{
		// Act
		string[] reads = [.. FindSystemClockReads(RepositoryFiles.EnumerateTestCode(), TestForms)];

		// Assert
		reads
			.Except(TestExceptions)
			.Should()
			.HaveCount(0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Matches a cancellation set to fire after a delay.
	/// </summary>
	[GeneratedRegex(@"\.\s*CancelAfter\s*\(")]
	private static partial Regex CancelAfterRegex();

	/// <summary>
	/// Matches a read of the current date or time.
	/// </summary>
	[GeneratedRegex(@"\bDateTime(?:Offset)?\s*\.\s*(?:Now|UtcNow|Today)\b")]
	private static partial Regex DateTimeNowRegex();

	/// <summary>
	/// Finds the reads of the system clock in source files, each as its file and its form.
	/// </summary>
	private static IEnumerable<string> FindSystemClockReads(
		IEnumerable<(string FilePath, string Text)> files,
		IReadOnlyDictionary<string, Regex> forms)
	{
		return CodeFormFinder
			.Find(files, forms)
			.Select(static x => $"{x.FilePath} reads {x.Form}");
	}

	/// <summary>
	/// Matches a busy wait.
	/// </summary>
	[GeneratedRegex(@"\bSpinWait\b")]
	private static partial Regex SpinWaitRegex();

	/// <summary>
	/// Matches a use of a stopwatch.
	/// </summary>
	[GeneratedRegex(@"\bStopwatch\b")]
	private static partial Regex StopwatchRegex();

	/// <summary>
	/// Matches a delay whose statement names no time provider.
	/// </summary>
	[GeneratedRegex(@"\bTask\s*\.\s*Delay\s*\((?![^;]*?(?i:timeprovider))")]
	private static partial Regex TaskDelayRegex();

	/// <summary>
	/// Matches a sleep of the current thread.
	/// </summary>
	[GeneratedRegex(@"\bThread\s*\.\s*Sleep\b")]
	private static partial Regex ThreadSleepRegex();

	/// <summary>
	/// Matches a wait bounded by a time span written in its statement, when the statement names no time provider.
	/// </summary>
	[GeneratedRegex(@"\.\s*WaitAsync\s*\((?![^;]*?(?i:timeprovider))[^;]*?\bTimeSpan\b")]
	private static partial Regex WaitAsyncRegex();
	#endregion
}
