using AwesomeAssertions;
using DataOrganizer.TestSupport.Guards;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace DataOrganizer.GuardTests.Guards;

[Guard]
[TestFixture(Description = "Guards the markup against namespace prefixes that nothing in the file uses")]
internal partial class UnusedXmlnsTests
{
	#region Methods
	/// <summary>
	/// Every prefix a markup file declares names an element or an attribute, or opens a name in a value: <c>prefix:</c>,
	/// or <c>prefix|</c> in a style selector. A bare listing in <c>mc:Ignorable</c> is no use.
	/// </summary>
	[Test]
	public void Markup_Uses_Every_Prefix_It_Declares()
	{
		// Arrange
		(string FilePath, XDocument Document)[] markup = [.. RepositoryFiles.EnumerateProjectMarkup()];

		// Act
		string[] unused =
		[
			.. markup.SelectMany(static file => FindUnused(file.Document)
				.Select(declaration => $"{Path.GetFileName(file.FilePath)}:{((IXmlLineInfo)declaration).LineNumber} {declaration.Name.LocalName}"))
		];

		// Assert
		markup
			.Should()
			.NotBeEmpty();

		unused
			.Should()
			.HaveCount(0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Finds the prefix declarations of a markup file whose prefix nothing in the file uses.
	/// </summary>
	private static IEnumerable<XAttribute> FindUnused(XDocument document)
	{
		XAttribute[] attributes = [.. document.Descendants().SelectMany(static x => x.Attributes())];

		HashSet<XNamespace> named =
		[
			.. document.Descendants().Select(static x => x.Name.Namespace),
			.. attributes.Where(static x => !x.IsNamespaceDeclaration).Select(static x => x.Name.Namespace)
		];

		HashSet<string> written =
		[
			.. attributes
				.Where(static x => !x.IsNamespaceDeclaration)
				.Select(static x => x.Value)
				.Concat(document.DescendantNodes().OfType<XText>().Select(static x => x.Value))
				.SelectMany(static x => PrefixUseRegex().Matches(x))
				.Select(static x => x.Groups["prefix"].Value)
		];

		return attributes.Where(x => x.IsNamespaceDeclaration
			&& x.Name.Namespace == XNamespace.Xmlns
			&& !named.Contains(XNamespace.Get(x.Value))
			&& !written.Contains(x.Name.LocalName));
	}

	/// <summary>
	/// Matches a prefix that opens a name in a value: before a colon, or before a bar in a style selector.
	/// </summary>
	[GeneratedRegex(@"(?<![\w.:-])(?<prefix>[A-Za-z_][\w-]*)[:|]")]
	private static partial Regex PrefixUseRegex();
	#endregion
}
