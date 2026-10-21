using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using AwesomeAssertions;
using DataOrganizer.Converters;
using DataOrganizer.Helpers.Views;
using DataOrganizer.UnitTests.Factories;
using System.Globalization;

namespace DataOrganizer.UnitTests.Converters;

[TestFixture(Description = $@"Tests of ""{nameof(AppConverters)}"" type")]
internal class AppConvertersTests
{
	#region Methods
	/// <summary>
	/// <see cref="AppConverters.FieldLabelAutomationName" />: a field with a label is named by the label, not by its placeholder.
	/// </summary>
	[Test]
	public void FieldLabelAutomationName_Gives_The_Label()
	{
		// Arrange
		const string label = "Password";

		// Act
		object? result = Convert(AppConverters.FieldLabelAutomationName, label, "Type here");

		// Assert
		result
			.Should()
			.Be(label);
	}

	/// <summary>
	/// <see cref="AppConverters.FieldLabelAutomationName" />: a field without a label is named by its placeholder.
	/// </summary>
	[TestCase(null)]
	[TestCase("")]
	public void FieldLabelAutomationName_Without_A_Label_Gives_The_Placeholder(string? label)
	{
		// Arrange
		const string placeholder = "Search";

		// Act
		object? result = Convert(AppConverters.FieldLabelAutomationName, label, placeholder);

		// Assert
		result
			.Should()
			.Be(placeholder);
	}

	/// <summary>
	/// <see cref="AppConverters.FieldLabelAutomationName" />: a field with neither a label nor a placeholder leaves the name
	/// unset.
	/// </summary>
	[Test]
	public void FieldLabelAutomationName_Without_Text_Leaves_The_Name_Unset()
	{
		// Act
		object? result = Convert(AppConverters.FieldLabelAutomationName, null, string.Empty);

		// Assert
		result
			.Should()
			.Be(AvaloniaProperty.UnsetValue);
	}

	/// <summary>
	/// <see cref="AppConverters.NamedItemAutomationName" />: the item of an object that has a name is named by it.
	/// </summary>
	[Test]
	public void NamedItemAutomationName_Gives_The_Name_Of_A_Named_Object()
	{
		// Arrange
		const string name = "main.c";

		// Act
		object? result = Convert(AppConverters.NamedItemAutomationName, ItemDtoFactory.CreateNamedFileDto(name));

		// Assert
		result
			.Should()
			.Be(name);
	}

	/// <summary>
	/// <see cref="AppConverters.NamedItemAutomationName" />: an object without a name leaves the name unset.
	/// </summary>
	[Test]
	public void NamedItemAutomationName_Without_A_Name_Is_Unset()
	{
		// Act
		object? result = Convert(AppConverters.NamedItemAutomationName, new object());

		// Assert
		result
			.Should()
			.Be(AvaloniaProperty.UnsetValue);
	}

	/// <summary>
	/// <see cref="AppConverters.PngToBitmap" />: the bytes of a PNG become its image.
	/// </summary>
	[AvaloniaTest]
	public void PngToBitmap_Builds_The_Image_Of_A_Png()
	{
		// Arrange
		const string onePixelPng = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";

		// Act
		object? result = Convert(AppConverters.PngToBitmap, System.Convert.FromBase64String(onePixelPng));

		// Assert
		result
			.Should()
			.BeOfType<Bitmap>();
	}

	/// <summary>
	/// <see cref="AppConverters.PngToBitmap" />: without bytes there is no image.
	/// </summary>
	[Test]
	public void PngToBitmap_Without_A_Png_Is_Empty()
	{
		// Act
		object? result = Convert(AppConverters.PngToBitmap, null);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="AppConverters.ScrollGutter" />: content that fits reserves no gutter.
	/// </summary>
	[TestCase(100.0)]
	[TestCase(50.0)]
	public void ScrollGutter_Reserves_Nothing_While_The_Content_Fits(double extent)
	{
		// Act
		object? result = Convert(AppConverters.ScrollGutter, extent, 100.0);

		// Assert
		result
			.Should()
			.Be(default(Thickness));
	}

	/// <summary>
	/// <see cref="AppConverters.ScrollGutter" />: content that overflows reserves the width of the scroll bar on the right.
	/// </summary>
	[Test]
	public void ScrollGutter_Reserves_The_Scroll_Bar_While_The_Content_Overflows()
	{
		// Act
		object? result = Convert(AppConverters.ScrollGutter, 200.0, 100.0);

		// Assert
		result
			.Should()
			.Be(new Thickness(0.0, 0.0, 10.0, 0.0));
	}

	/// <summary>
	/// <see cref="AppConverters.SizeChangedToWidth" />: a changed size gives its new width.
	/// </summary>
	[Test]
	public void SizeChangedToWidth_Gives_The_New_Width()
	{
		// Arrange
		SizeChangedEventArgs args = new(null, null, new Size(500.0, 300.0), new Size(250.0, 400.0));

		// Act
		object? result = Convert(AppConverters.SizeChangedToWidth, args);

		// Assert
		result
			.Should()
			.Be(250.0);
	}

	/// <summary>
	/// <see cref="AppConverters.SizeChangedToWidth" />: no change gives no width.
	/// </summary>
	[Test]
	public void SizeChangedToWidth_Without_A_Change_Is_Not_A_Number()
	{
		// Act
		object? result = Convert(AppConverters.SizeChangedToWidth, null);

		// Assert
		result
			.Should()
			.Be(double.NaN);
	}

	/// <summary>
	/// <see cref="AppConverters.ToolTipAutomationName" />: a button with text keeps the text as its name.
	/// </summary>
	[Test]
	public void ToolTipAutomationName_Leaves_A_Button_With_Text_To_Its_Text()
	{
		// Act
		object? result = Convert(AppConverters.ToolTipAutomationName, "Save", "Add");

		// Assert
		result
			.Should()
			.Be(AvaloniaProperty.UnsetValue);
	}

	/// <summary>
	/// <see cref="AppConverters.ToolTipAutomationName" />: a button that shows a glyph, such as an emoji, is named by its tip.
	/// </summary>
	[TestCase(Glyphs.Clipboard)]
	[TestCase(Glyphs.AngleBracketSlash)]
	public void ToolTipAutomationName_Names_A_Button_With_A_Glyph_By_Its_Tip(string glyph)
	{
		// Arrange
		const string tip = "All";

		// Act
		object? result = Convert(AppConverters.ToolTipAutomationName, glyph, tip);

		// Assert
		result
			.Should()
			.Be(tip);
	}

	/// <summary>
	/// <see cref="AppConverters.ToolTipAutomationName" />: a button that shows no text is named by its tip.
	/// </summary>
	[TestCase(true)]
	[TestCase(false)]
	public void ToolTipAutomationName_Names_A_Button_Without_Text_By_Its_Tip(bool hasContent)
	{
		// Arrange
		const string tip = "Add";

		// An object stands for an icon or any other content that is not text.
		object? content = hasContent
			? new object()
			: null;

		// Act
		object? result = Convert(AppConverters.ToolTipAutomationName, content, tip);

		// Assert
		result
			.Should()
			.Be(tip);
	}

	/// <summary>
	/// <see cref="AppConverters.ToolTipAutomationName" />: a tip that is not text leaves the name unset.
	/// </summary>
	[TestCase(true)]
	[TestCase(false)]
	public void ToolTipAutomationName_Without_A_Text_Tip_Leaves_The_Name_Unset(bool hasTip)
	{
		// Arrange
		// An object stands for a tip built from a template.
		object? tip = hasTip
			? new object()
			: null;

		// Act
		object? result = Convert(AppConverters.ToolTipAutomationName, new object(), tip);

		// Assert
		result
			.Should()
			.Be(AvaloniaProperty.UnsetValue);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Runs a converter over the label and the placeholder of an input field.
	/// </summary>
	private static object? Convert(FieldLabelAutomationNameConverter converter, string? label, string? placeholder)
	{
		return converter.Convert(
			[label, placeholder],
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over the extent and the viewport heights of a scroll viewer.
	/// </summary>
	private static object? Convert(FuncMultiValueConverter<double, Thickness> converter, double extent, double viewport)
	{
		return converter.Convert(
			[extent, viewport],
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over the object of an item.
	/// </summary>
	private static object? Convert(FuncValueConverter<object?, object?> converter, object? item)
	{
		return converter.Convert(
			item,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over the bytes of a PNG.
	/// </summary>
	private static object? Convert(FuncValueConverter<byte[]?, Bitmap?> converter, byte[]? png)
	{
		return converter.Convert(
			png,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over a change of the size of a control.
	/// </summary>
	private static object? Convert(FuncValueConverter<SizeChangedEventArgs?, double> converter, SizeChangedEventArgs? args)
	{
		return converter.Convert(
			args,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over the content and the tip of a button.
	/// </summary>
	private static object? Convert(ToolTipAutomationNameConverter converter, object? content, object? tip)
	{
		return converter.Convert(
			[content, tip],
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}
	#endregion
}
