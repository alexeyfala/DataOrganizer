using DataOrganizer.TestSupport.Guards;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DataOrganizer.GuardTests.TestHelpers;

/// <summary>
/// The resources of the localized strings of the application: the neutral file and its translations.
/// </summary>
internal static class LocalizedResources
{
	#region Data
	/// <summary>
	/// Folder of the resources, relative to the repository root.
	/// </summary>
	private const string Folder = "src/DataOrganizer.Shared/Properties";

	/// <summary>
	/// File of the resources of the neutral language.
	/// </summary>
	private const string NeutralFileName = "Strings.resx";

	/// <summary>
	/// Resources of the neutral language, relative to the repository root.
	/// </summary>
	public const string NeutralPath = $"{Folder}/{NeutralFileName}";

	/// <summary>
	/// Resources of the Russian language, relative to the repository root.
	/// </summary>
	public const string RussianPath = $"{Folder}/Strings.ru.resx";
	#endregion

	#region Methods
	/// <summary>
	/// Enumerates the translations of the neutral resources, each relative to the repository root.
	/// </summary>
	public static IEnumerable<string> EnumerateTranslationPaths()
	{
		return Directory
			.EnumerateFiles(Path.Combine(RepositoryFiles.LocateRoot(), Folder), "Strings.*.resx")
			.Select(Path.GetFileName)
			.Where(static x => x != NeutralFileName)
			.Select(static x => $"{Folder}/{x}");
	}

	/// <summary>
	/// Reads the entries of a resource file, each as its name and value.
	/// </summary>
	public static IEnumerable<(string Name, string Value)> ReadEntries(string relativePath)
	{
		return XDocument
			.Parse(RepositoryFiles.ReadText(relativePath))
			.Root!
			.Elements("data")
			.Select(static x => (Name: x.Attribute("name")!.Value, x.Element("value")!.Value));
	}
	#endregion
}
