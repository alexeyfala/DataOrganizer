using Avalonia;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DataOrganizer.UnitTests.Guards;

[Guard]
[TestFixture(Description = "Guards that every resource key the markup references can still be resolved")]
internal partial class ResourceKeyConsistencyTests
{
	#region Data
	/// <summary>
	/// Attribute naming a resource in the element form of a reference.
	/// </summary>
	private const string ResourceKeyAttributeName = "ResourceKey";

	/// <summary>
	/// Namespace of the <c>x:Key</c> attribute that declares a resource.
	/// </summary>
	private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
	#endregion

	#region Methods
	/// <summary>
	/// Every resource key the markup references is either declared by the application markup
	/// or served by a theme the application merges in.
	/// </summary>
	/// <remarks>
	/// A missing key is invisible to the compiler, so nothing but this test catches a rename
	/// that left a reference behind.
	/// </remarks>
	[AvaloniaTest]
	public void Referenced_Resource_Keys_Are_Declared_Or_Served_By_A_Theme()
	{
		// Arrange
		(string FilePath, XDocument Document)[] markup = [.. RepositoryFiles.EnumerateProjectMarkup()];

		HashSet<string> declared = [.. markup.SelectMany(x => CollectDeclaredKeys(x.Document))];

		(string FilePath, string Key)[] references = [.. markup.SelectMany(CollectReferences)];

		// Act
		string[] unresolved = [.. references
			.Where(x => !declared.Contains(x.Key) && !IsServedByTheme(x.Key))
			.Select(x => $"{Path.GetFileName(x.FilePath)}: {x.Key}")
			.Distinct(StringComparer.Ordinal)
			.Order(StringComparer.Ordinal)];

		// Assert
		references
			.Should()
			.NotBeEmpty();

		unresolved
			.Should()
			.BeEmpty();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the resource keys the markup declares.
	/// </summary>
	private static IEnumerable<string> CollectDeclaredKeys(XDocument document)
	{
		return document
			.Descendants()
			.Select(element => element.Attribute(XamlNamespace + "Key"))
			.Where(attribute => attribute is not null)
			.Select(attribute => attribute!.Value);
	}

	/// <summary>
	/// Returns the resource keys the markup references, each paired with the file it came from.
	/// </summary>
	private static IEnumerable<(string FilePath, string Key)> CollectReferences((string FilePath, XDocument Document) markup)
	{
		foreach (XElement element in markup.Document.Descendants())
		{
			if (element.Attribute(ResourceKeyAttributeName) is { } resourceKey)
			{
				yield return (markup.FilePath, resourceKey.Value);
			}

			foreach (XAttribute attribute in element.Attributes())
			{
				foreach (Match match in ReferenceRegex().Matches(attribute.Value))
				{
					yield return (markup.FilePath, match.Groups["key"].Value);
				}
			}
		}
	}

	/// <summary>
	/// <c>True</c> when the running application resolves the key from a merged theme.
	/// </summary>
	private static bool IsServedByTheme(string key)
	{
		Application application = Application.Current!;

		return application.TryGetResource(key, application.ActualThemeVariant, out _);
	}

	/// <summary>
	/// Matches a StaticResource or DynamicResource reference and captures the key.
	/// </summary>
	[GeneratedRegex(@"\{(?:Static|Dynamic)Resource\s+(?<key>[^}\s]+)\s*\}")]
	private static partial Regex ReferenceRegex();
	#endregion
}
