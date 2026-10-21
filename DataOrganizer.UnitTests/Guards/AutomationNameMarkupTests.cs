using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace DataOrganizer.UnitTests.Guards;

[Guard]
[TestFixture(Description = "Guards that the markup gives every interactive element a name for UI Automation")]
internal partial class AutomationNameMarkupTests
{
	#region Data
	/// <summary>
	/// Attribute that names an element for UI Automation.
	/// </summary>
	private const string AutomationNameProperty = "AutomationProperties.Name";

	/// <summary>
	/// Attribute that names an element for UI Automation by another element.
	/// </summary>
	private const string LabeledByProperty = "AutomationProperties.LabeledBy";

	/// <summary>
	/// Attribute, property element or setter of a tip, which names a button without text.
	/// </summary>
	private const string ToolTipProperty = "ToolTip.Tip";

	/// <summary>
	/// Kinds of buttons, which a tip or an automation name names when they show no text.
	/// </summary>
	private static readonly HashSet<string> ButtonKinds = new(StringComparer.Ordinal)
	{
		"Button",
		"CheckBox",
		"DropDownButton",
		"FlyoutButton",
		"HyperlinkButton",
		"RadioButton",
		"RepeatButton",
		"SplitButton",
		"ToggleButton",
		"ToggleSwitch"
	};

	/// <summary>
	/// Kinds of items, which only an automation name names when they show no text.
	/// </summary>
	private static readonly HashSet<string> ItemKinds = new(StringComparer.Ordinal)
	{
		"ComboBoxItem",
		"ListBoxItem",
		"MenuItem",
		"TabItem",
		"TreeViewItem"
	};

	/// <summary>
	/// Attributes and setters that name a text box: a label, a placeholder or the name itself.
	/// </summary>
	private static readonly HashSet<string> TextBoxNamers = new(StringComparer.Ordinal)
	{
		AutomationNameProperty,
		LabeledByProperty,
		"PlaceholderText",
		"TextFieldAssist.Label",
		"Watermark"
	};
	#endregion

	#region Methods
	/// <summary>
	/// A button that shows an icon, a glyph or a panel has a tip of text or an automation name; a bound tip does not
	/// count, as it may give no text.
	/// </summary>
	[Test]
	public void Buttons_Without_Text_Have_A_Tip_Or_A_Name()
	{
		// Arrange
		(string FilePath, XElement Element)[] buttons = [.. FindElements(ButtonKinds)];

		// Act
		string[] unnamed = [.. buttons
			.Where(x => ShowsText(x.Element) is false && !HasAny(x.Element, AutomationNameProperty, LabeledByProperty) && !HasTextTip(x.Element))
			.Select(Describe)];

		// Assert
		buttons
			.Should()
			.NotBeEmpty();

		unnamed
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// A combo box has an automation name or a placeholder, as it shows the chosen item and not what it chooses.
	/// </summary>
	[Test]
	public void Combo_Boxes_Have_A_Name()
	{
		// Arrange
		(string FilePath, XElement Element)[] comboBoxes = [.. FindElements(["ComboBox"])];

		// Act
		string[] unnamed = [.. comboBoxes
			.Where(x => !HasAny(x.Element, AutomationNameProperty, LabeledByProperty, "PlaceholderText"))
			.Select(Describe)];

		// Assert
		comboBoxes
			.Should()
			.NotBeEmpty();

		unnamed
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// An item of a list, a menu or tabs that shows a panel has an automation name.
	/// </summary>
	[Test]
	public void Items_Without_Text_Have_A_Name()
	{
		// Arrange
		(string FilePath, XElement Element)[] items = [.. FindElements(ItemKinds)];

		// Act
		string[] unnamed = [.. items
			.Where(x => ShowsText(x.Element) is false && !HasAny(x.Element, AutomationNameProperty, LabeledByProperty))
			.Select(Describe)];

		// Assert
		items
			.Should()
			.NotBeEmpty();

		unnamed
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// A text box has a label, a placeholder or an automation name, set on it or by a style class.
	/// </summary>
	[Test]
	public void Text_Boxes_Have_A_Label_A_Placeholder_Or_A_Name()
	{
		// Arrange
		(string FilePath, XDocument Document)[] markup = [.. RepositoryFiles.EnumerateProjectMarkup()];

		HashSet<string> namingClasses = [.. markup.SelectMany(x => FindNamingClasses(x.Document))];

		(string FilePath, XElement Element)[] textBoxes = [.. FindElements(["TextBox"])];

		// Act
		string[] unnamed = [.. textBoxes
			.Where(x => !HasAny(x.Element, [.. TextBoxNamers]) && !GetClasses(x.Element).Overlaps(namingClasses))
			.Select(Describe)];

		// Assert
		textBoxes
			.Should()
			.NotBeEmpty();

		unnamed
			.Should()
			.BeEmpty();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the file and the line of an element with its kind.
	/// </summary>
	private static string Describe((string FilePath, XElement Element) item)
	{
		return $"{Path.GetFileName(item.FilePath)}:{((IXmlLineInfo)item.Element).LineNumber} {item.Element.Name.LocalName}";
	}

	/// <summary>
	/// Returns the elements of the given kinds in the markup of the application, each paired with its file.
	/// </summary>
	private static IEnumerable<(string FilePath, XElement Element)> FindElements(IReadOnlyCollection<string> kinds)
	{
		return RepositoryFiles
			.EnumerateProjectMarkup()
			.SelectMany(markup => markup
				.Document
				.Descendants()
				.Where(element => kinds.Contains(element.Name.LocalName))
				.Select(element => (markup.FilePath, element)));
	}

	/// <summary>
	/// Returns the classes of text boxes whose styles set a label, a placeholder or an automation name.
	/// </summary>
	private static IEnumerable<string> FindNamingClasses(XDocument document)
	{
		return document
			.Descendants()
			.Where(element => element.Name.LocalName == "Style"
				&& element
					.Elements()
					.Any(setter => setter.Name.LocalName == "Setter" && TextBoxNamers.Contains(LocalName(setter.Attribute("Property")?.Value))))
			.SelectMany(style => TextBoxClassRegex()
				.Matches(style.Attribute("Selector")?.Value ?? string.Empty)
				.Select(match => match.Groups[1].Value));
	}

	/// <summary>
	/// Returns the values the element gives a property by an attribute, a property element or a setter of its own styles;
	/// a property element that holds elements, or a setter without a value attribute, gives <c>null</c>.
	/// </summary>
	private static IEnumerable<string?> FindValues(XElement element, string name)
	{
		foreach (XAttribute attribute in element.Attributes().Where(attribute => attribute.Name.LocalName == name))
		{
			yield return attribute.Value;
		}

		foreach (XElement property in element.Elements().Where(child => child.Name.LocalName == name))
		{
			yield return property.HasElements ? null : property.Value;
		}

		foreach (XElement setter in element
			.Elements()
			.Where(child => child.Name.LocalName.EndsWith(".Styles", StringComparison.Ordinal))
			.Descendants()
			.Where(setter => setter.Name.LocalName == "Setter" && LocalName(setter.Attribute("Property")?.Value) == name))
		{
			yield return setter.Attribute("Value")?.Value;
		}
	}

	/// <summary>
	/// Returns the style classes an element lists in its <c>Classes</c> attribute.
	/// </summary>
	private static HashSet<string> GetClasses(XElement element)
	{
		return [.. (element.Attribute("Classes")?.Value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries)];
	}

	/// <summary>
	/// Whether the element sets any of the given properties by an attribute, a property element or a setter of its own styles.
	/// </summary>
	private static bool HasAny(XElement element, params string[] names) => names.Any(name => FindValues(element, name).Any());

	/// <summary>
	/// Whether the element sets a tip of text, which names a button: a literal or a static string.
	/// </summary>
	private static bool HasTextTip(XElement element) => FindValues(element, ToolTipProperty).Any(IsTextTip);

	/// <summary>
	/// Whether element content names its control: a lone text block does as content, which Avalonia reads, not as a header.
	/// </summary>
	private static bool IsTextContent(string contentName, IEnumerable<XElement> children)
	{
		return contentName == "Content" && children.ToArray() is [{ Name.LocalName: "TextBlock" or "SelectableTextBlock" }];
	}

	/// <summary>
	/// Whether the value of a tip is text: a literal or a static string. A binding may give no text at run time, and a
	/// tip that is not text names nothing.
	/// </summary>
	private static bool IsTextTip(string? value)
	{
		return value is not null && (!value.StartsWith('{') || value.StartsWith("{x:Static ", StringComparison.Ordinal));
	}

	/// <summary>
	/// Whether the value of a content attribute is text, unlike an icon or a glyph.
	/// </summary>
	private static bool IsTextValue(string value)
	{
		if (value.StartsWith("{materialIcons:", StringComparison.Ordinal))
		{
			return value.Contains("Text=", StringComparison.Ordinal);
		}

		return !value.Contains("Glyphs.", StringComparison.Ordinal);
	}

	/// <summary>
	/// Returns the name of a property without the prefix of its namespace, such as <c>TextFieldAssist.Label</c>.
	/// </summary>
	private static string LocalName(string? property) => property?[(property.IndexOf(':') + 1)..] ?? string.Empty;

	/// <summary>
	/// Whether the element shows text: <c>true</c> for text, <c>false</c> for an icon, a glyph or a panel, and
	/// <c>null</c> when its content comes from a theme or a style.
	/// </summary>
	private static bool? ShowsText(XElement element)
	{
		string kind = element.Name.LocalName;

		// A flyout button builds its content from the header, and a headered item shows its header.
		string contentName = kind is "FlyoutButton" or "MenuItem" or "TabItem" or "TreeViewItem"
			? "Header"
			: "Content";

		if (element.Attribute(contentName) is { } attribute)
		{
			return IsTextValue(attribute.Value);
		}

		if (element.Element(element.Name.Namespace + $"{kind}.{contentName}") is { } property)
		{
			return property.HasElements
				? IsTextContent(contentName, property.Elements())
				: property.Value.Trim().Length > 0;
		}

		// A flyout button without a header shows its icon alone.
		if (kind == "FlyoutButton")
		{
			return false;
		}

		XElement[] children = [.. element
			.Elements()
			.Where(child => !child.Name.LocalName.Contains('.'))];

		if (children.Length > 0)
		{
			return IsTextContent(contentName, children);
		}

		return element
			.Nodes()
			.OfType<XText>()
			.Any(text => text.Value.Trim().Length > 0)
			? true
			: null;
	}

	/// <summary>
	/// Matches the class of a text box in a style selector, such as <c>TextBox.SearchTextBoxStyle</c>.
	/// </summary>
	[GeneratedRegex(@"TextBox\.(\w+)")]
	private static partial Regex TextBoxClassRegex();
	#endregion
}
