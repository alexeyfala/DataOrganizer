using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DataOrganizer.GuardTests.TestHelpers;

/// <summary>
/// Finds in source files the forms of code a guard rules out.
/// </summary>
internal static class CodeFormFinder
{
	#region Methods
	/// <summary>
	/// Finds the forms each file uses, its comments and literals left out, as the file and the name of the form.
	/// </summary>
	public static IEnumerable<(string FilePath, string Form)> Find(
		IEnumerable<(string FilePath, string Text)> files,
		IReadOnlyDictionary<string, Regex> forms)
	{
		foreach ((string path, string text) in files)
		{
			string code = CodeText.WithoutCommentsAndLiterals(text);

			foreach ((string name, Regex form) in forms)
			{
				if (form.IsMatch(code))
				{
					yield return (path, name);
				}
			}
		}
	}
	#endregion
}
