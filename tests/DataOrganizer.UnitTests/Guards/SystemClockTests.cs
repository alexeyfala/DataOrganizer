using AwesomeAssertions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.UnitTests.Guards;

[Guard]
[TestFixture(Description = "Guards the code against reading the time past the time provider")]
internal partial class SystemClockTests
{
	#region Data
	/// <summary>
	/// The reads of the system clock that stay, each as the file and the form it uses: the crash log is written before
	/// the services exist.
	/// </summary>
	private static readonly string[] Exceptions =
	[
		"DataOrganizer/App.axaml.cs reads DateTime.Now"
	];

	/// <summary>
	/// The forms of reading the system clock, each under the name a failure shows. A delay or a wait counts when its
	/// statement names no time provider. A cancellation set with <c>CancelAfter</c> always counts: the call does not
	/// show whether its source runs on a time provider.
	/// </summary>
	private static readonly Dictionary<string, Regex> Forms = new()
	{
		["CancelAfter"] = CancelAfterRegex(),
		["DateTime.Now"] = DateTimeNowRegex(),
		["Stopwatch"] = StopwatchRegex(),
		["Task.Delay"] = TaskDelayRegex(),
		["Thread.Sleep"] = ThreadSleepRegex(),
		["WaitAsync"] = WaitAsyncRegex()
	};
	#endregion

	#region Methods
	/// <summary>
	/// No file reads the system clock on its own, the listed exceptions left out: the time comes from the injected
	/// <see cref="System.TimeProvider" />, which a test replaces.
	/// </summary>
	[Test]
	public void Code_Reads_The_Time_Only_Through_The_Time_Provider()
	{
		// Act
		string[] reads = [.. FindSystemClockReads()];

		// Assert
		reads
			.Except(Exceptions)
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Every listed exception still reads the system clock, so the list keeps no entry that is no longer needed.
	/// </summary>
	[Test]
	public void Every_Exception_Still_Reads_The_System_Clock()
	{
		// Act
		string[] reads = [.. FindSystemClockReads()];

		// Assert
		Exceptions
			.Except(reads)
			.Should()
			.BeEmpty();
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
	/// Enumerates the C# files of a folder, the build output left out.
	/// </summary>
	private static IEnumerable<string> EnumerateCode(string root)
	{
		return Directory
			.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
			.Where(path => !IsBuildOutput(Path.GetRelativePath(root, path)));
	}

	/// <summary>
	/// Finds the reads of the system clock in the projects of the application, each as its file and its form.
	/// </summary>
	private static IEnumerable<string> FindSystemClockReads()
	{
		string root = Path.Combine(RepositoryFiles.LocateRoot(), RepositoryFiles.SourceFolder);

		foreach (string file in EnumerateCode(root))
		{
			string code = CodeText.WithoutCommentsAndLiterals(File.ReadAllText(file));

			string path = Path
				.GetRelativePath(root, file)
				.Replace(Path.DirectorySeparatorChar, '/');

			foreach ((string name, Regex form) in Forms)
			{
				if (form.IsMatch(code))
				{
					yield return $"{path} reads {name}";
				}
			}
		}
	}

	/// <summary>
	/// Tells whether a file, given by its path inside the source folder, lies in the build output.
	/// </summary>
	private static bool IsBuildOutput(string relativePath)
	{
		return relativePath
			.Split(Path.DirectorySeparatorChar)
			.Any(static x => x is "bin" or "obj");
	}

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
