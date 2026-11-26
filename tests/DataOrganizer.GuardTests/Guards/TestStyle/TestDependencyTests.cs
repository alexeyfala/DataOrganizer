using AwesomeAssertions;
using DataOrganizer.GuardTests.TestHelpers;
using DataOrganizer.TestSupport.Guards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.GuardTests.Guards.TestStyle;

[Guard]
[TestFixture(Description = "Guards the tests against libraries, texts and the disk that the test style rules out")]
internal sealed partial class TestDependencyTests
{
	#region Data
	/// <summary>
	/// Ending of the name of a unit test project.
	/// </summary>
	private const string UnitTestProjectSuffix = ".UnitTests/";

	/// <summary>
	/// The forms of reaching the disk past a mock file system, each under the name a failure shows.
	/// </summary>
	private static readonly Dictionary<string, Regex> DiskForms = new()
	{
		["Directory"] = DirectoryRegex(),
		["File"] = FileRegex(),
		["FileInfo or DirectoryInfo"] = FileInfoRegex(),
		["Path.GetTempPath"] = TempPathRegex()
	};

	/// <summary>
	/// The forms of reading a localized text, each under the name a failure shows. The culture of the texts and the name
	/// of a key inside <c>nameof</c> stay allowed.
	/// </summary>
	private static readonly Dictionary<string, Regex> LocalizedTextForms = new()
	{
		["Strings"] = LocalizedStringRegex()
	};

	/// <summary>
	/// The forms of the Moq API, each under the name a failure shows.
	/// </summary>
	private static readonly Dictionary<string, Regex> MoqForms = new()
	{
		["It.Is"] = ArgumentMatcherRegex(),
		["Mock<T>"] = MockRegex(),
		["Setup"] = SetupRegex(),
		["using Moq"] = UsingMoqRegex(),
		["Verify"] = VerifyRegex()
	};
	#endregion

	#region Methods
	/// <summary>
	/// No test reads a localized text: a test checks what the code did, not what it says.
	/// </summary>
	[Test]
	public void Tests_Do_Not_Read_Localized_Texts()
	{
		// Act
		string[] uses = [.. FindUses(RepositoryFiles.EnumerateTestCode(), LocalizedTextForms)];

		// Assert
		uses
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// No test uses the API of Moq: doubles are substitutes configured inside the setup of the container.
	/// </summary>
	[Test]
	public void Tests_Do_Not_Use_Moq()
	{
		// Act
		string[] uses = [.. FindUses(RepositoryFiles.EnumerateTestCode(), MoqForms)];

		// Assert
		uses
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// No unit test reaches the disk: work with files runs on a mock file system.
	/// </summary>
	[Test]
	public void Unit_Tests_Keep_Off_The_Disk()
	{
		// Arrange
		(string FilePath, string Text)[] unitTestCode =
		[
			.. RepositoryFiles
				.EnumerateTestCode()
				.Where(static x => x.FilePath.Contains(UnitTestProjectSuffix, StringComparison.Ordinal))
		];

		// Act
		string[] uses = [.. FindUses(unitTestCode, DiskForms)];

		// Assert
		unitTestCode
			.Should()
			.NotBeEmpty();

		uses
			.Should()
			.HaveCount(0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Matches an argument matcher of Moq.
	/// </summary>
	[GeneratedRegex(@"\bIt\s*\.\s*Is\w*\s*[<(]")]
	private static partial Regex ArgumentMatcherRegex();

	/// <summary>
	/// Matches a static member of the directories of the disk.
	/// </summary>
	[GeneratedRegex(@"(?<![\w.])(?:System\s*\.\s*IO\s*\.\s*)?Directory\s*\.\s*[A-Z]")]
	private static partial Regex DirectoryRegex();

	/// <summary>
	/// Matches a file or a directory of the disk built with <c>new</c>.
	/// </summary>
	[GeneratedRegex(@"\bnew\s+(?:FileInfo|DirectoryInfo)\b")]
	private static partial Regex FileInfoRegex();

	/// <summary>
	/// Matches a static member of the files of the disk.
	/// </summary>
	[GeneratedRegex(@"(?<![\w.])(?:System\s*\.\s*IO\s*\.\s*)?File\s*\.\s*[A-Z]")]
	private static partial Regex FileRegex();

	/// <summary>
	/// Finds the files that use a form, each as the file and the name of the form.
	/// </summary>
	private static IEnumerable<string> FindUses(
		IEnumerable<(string FilePath, string Text)> files,
		IReadOnlyDictionary<string, Regex> forms)
	{
		return CodeFormFinder
			.Find(files, forms)
			.Select(static x => $"{x.FilePath} uses {x.Form}");
	}

	/// <summary>
	/// Matches a key of the localized texts, their culture and the name of a key inside <c>nameof</c> left out.
	/// </summary>
	[GeneratedRegex(@"(?<!\bnameof\s*\(\s*(?:\w+\s*\.\s*)*)\bStrings\s*\.\s*(?!Culture\b)[A-Z]\w*")]
	private static partial Regex LocalizedStringRegex();

	/// <summary>
	/// Matches a mock of Moq.
	/// </summary>
	[GeneratedRegex(@"\bMock\s*<")]
	private static partial Regex MockRegex();

	/// <summary>
	/// Matches a setup of Moq.
	/// </summary>
	[GeneratedRegex(@"\.\s*Setup(?:Get|Set|Sequence|Property)?\s*\(")]
	private static partial Regex SetupRegex();

	/// <summary>
	/// Matches a read of the temporary folder of the disk.
	/// </summary>
	[GeneratedRegex(@"\bPath\s*\.\s*GetTemp(?:Path|FileName)\b")]
	private static partial Regex TempPathRegex();

	/// <summary>
	/// Matches the import of the namespace of Moq.
	/// </summary>
	[GeneratedRegex(@"\busing\s+Moq\b")]
	private static partial Regex UsingMoqRegex();

	/// <summary>
	/// Matches a verification of Moq.
	/// </summary>
	[GeneratedRegex(@"\.\s*Verify(?:All|Get|Set|NoOtherCalls)?\s*\(")]
	private static partial Regex VerifyRegex();
	#endregion
}
