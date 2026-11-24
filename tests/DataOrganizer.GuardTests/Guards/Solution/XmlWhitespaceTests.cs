using AwesomeAssertions;
using DataOrganizer.TestSupport.Guards;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.GuardTests.Guards.Solution;

[Guard]
[TestFixture(Description = "Guards the whitespace of the XML documents edited by hand")]
internal sealed class XmlWhitespaceTests
{
	#region Data
	/// <summary>
	/// Patterns of the XML documents edited by hand. The Avalonia markup follows XAML Styler, and the resources, the
	/// solution file and the images are written by their tools.
	/// </summary>
	private static readonly string[] XmlFilePatterns =
	[
		"*.config",
		"*.csproj",
		"*.manifest",
		"*.plist",
		"*.props",
		"*.targets",
		"*.wixproj",
		"*.wxi",
		"*.wxl",
		"*.wxs",
		"*.xml",
	];
	#endregion

	#region Methods
	/// <summary>
	/// The indentation of every line is made of tabs only.
	/// </summary>
	[Test]
	public void Lines_Are_Indented_With_Tabs_Only()
	{
		// Act
		string[] violations = [.. FindLines(static line => line.TrimStart('\t').StartsWith(' '))];

		// Assert
		violations
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// No line ends with a space or a tab, so a blank line is empty.
	/// </summary>
	[Test]
	public void Lines_End_Without_Whitespace()
	{
		// Act
		string[] violations = [.. FindLines(static line => line.EndsWith(' ') || line.EndsWith('\t'))];

		// Assert
		violations
			.Should()
			.HaveCount(0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Names the place of each line of the XML documents that matches the predicate, as a path and a line number.
	/// </summary>
	private static IEnumerable<string> FindLines(Func<string, bool> predicate)
	{
		return RepositoryFiles
			.EnumerateRepositoryFiles(XmlFilePatterns)
			.SelectMany(file => file.Text
				.Split('\n')
				.Select(static (line, index) => (Line: line.TrimEnd('\r'), Number: index + 1))
				.Where(x => predicate(x.Line))
				.Select(x => $"{file.FilePath}:{x.Number}"));
	}
	#endregion
}
