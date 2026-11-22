using AwesomeAssertions;
using DataOrganizer.GuardTests.TestHelpers;
using DataOrganizer.TestSupport.Guards;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.GuardTests.Guards;

[Guard]
[TestFixture(Description = "Guards the view models against the types of the UI framework")]
internal class ViewModelUiFrameworkTests
{
	#region Data
	/// <summary>
	/// Root namespaces of the UI framework and of its control libraries.
	/// </summary>
	private static readonly string[] UiNamespaces = ["Avalonia", "AvaloniaEdit"];

	/// <summary>
	/// Folder of the view models, as it stands inside a file path.
	/// </summary>
	private static readonly string ViewModelsFolder = $"{Path.DirectorySeparatorChar}ViewModels{Path.DirectorySeparatorChar}";
	#endregion

	#region Methods
	/// <summary>
	/// No view model has a <c>using</c> of a namespace of the UI framework or names a type of it in full: what a view
	/// model needs from a control or a window, the view or a behavior does.
	/// </summary>
	[Test]
	public void View_Models_Name_No_Namespace_Of_The_UI_Framework()
	{
		// Arrange
		Regex uiName = new($@"(?<![\w.])(?<name>(?:{string.Join('|', UiNamespaces)})(?:\.\w+)*)(?=\s*[.;])");

		// Act
		string[] violations =
		[
			.. RepositoryFiles
				.EnumerateProjectCode()
				.Where(static x => x.FilePath.Contains(ViewModelsFolder, StringComparison.Ordinal))
				.SelectMany(x => uiName
					.Matches(CodeText.WithoutCommentsAndLiterals(x.Text))
					.Select(static match => match.Groups["name"].Value)
					.Distinct()
					.Select(name => $"{Path.GetFileName(x.FilePath)} names {name}"))
		];

		// Assert
		violations
			.Should()
			.HaveCount(0);
	}
	#endregion
}
