using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AwesomeAssertions;
using DataOrganizer.Converters;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Enums.Settings;
using DataOrganizer.Extensions;
using DataOrganizer.UnitTests.Factories;
using System;
using System.Globalization;
using System.Linq;

namespace DataOrganizer.UnitTests.Converters;

[TestFixture(Description = $@"Tests of ""{nameof(AppConverters)}"" type")]
internal class AppConvertersTests
{
	#region Methods
	/// <summary>
	/// <see cref="AppConverters.AutoLockDelay" />: a delay of zero minutes or less stands for no auto-lock and is named
	/// apart from a real delay.
	/// </summary>
	[Test]
	public void AutoLockDelay_Names_Every_Missing_Delay_Alike()
	{
		// Act
		object? result = Convert(AppConverters.AutoLockDelay, -5);

		// Assert
		result
			.Should()
			.Be(Convert(AppConverters.AutoLockDelay, 0))
			.And
			.NotBe(Convert(AppConverters.AutoLockDelay, 1));
	}

	/// <summary>
	/// <see cref="AppConverters.AutoLockDelay" />: a delay is shown with its minutes.
	/// </summary>
	[Test]
	public void AutoLockDelay_Shows_The_Minutes()
	{
		// Act
		object? result = Convert(AppConverters.AutoLockDelay, 15);

		// Assert
		result
			.Should()
			.BeOfType<string>()
			.Which
			.Should()
			.Contain("15");
	}

	/// <summary>
	/// <see cref="AppConverters.AutoLockIsExpiring" />: the warning starts within the last twenty seconds.
	/// </summary>
	[TestCase(21.0, false)]
	[TestCase(20.0, true)]
	[TestCase(1.0, true)]
	public void AutoLockIsExpiring_Warns_About_The_Last_Seconds(double seconds, bool expected)
	{
		// Act
		object? result = Convert(AppConverters.AutoLockIsExpiring, TimeSpan.FromSeconds(seconds));

		// Assert
		result
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="AppConverters.AutoLockIsExpiring" />: a countdown that is not running never warns.
	/// </summary>
	[Test]
	public void AutoLockIsExpiring_Without_A_Countdown_Is_False()
	{
		// Act
		object? result = Convert(AppConverters.AutoLockIsExpiring, null);

		// Assert
		result
			.Should()
			.Be(false);
	}

	/// <summary>
	/// <see cref="AppConverters.AutoLockRemaining" />: an hour or more keeps its hours in the caption.
	/// </summary>
	[Test]
	public void AutoLockRemaining_Keeps_The_Hours_Of_The_Longest_Delay()
	{
		// Act
		object? result = Convert(AppConverters.AutoLockRemaining, TimeSpan.FromMinutes(60.0));

		// Assert
		result
			.Should()
			.BeOfType<string>()
			.Which
			.Should()
			.Contain("1:00:00");
	}

	/// <summary>
	/// <see cref="AppConverters.AutoLockRemaining" />: the time left is shown as minutes and seconds.
	/// </summary>
	[Test]
	public void AutoLockRemaining_Shows_Minutes_And_Seconds()
	{
		// Act
		object? result = Convert(AppConverters.AutoLockRemaining, TimeSpan.FromSeconds(577.0));

		// Assert
		result
			.Should()
			.BeOfType<string>()
			.Which
			.Should()
			.Contain("09:37");
	}

	/// <summary>
	/// <see cref="AppConverters.AutoLockRemaining" />: a countdown that is not running has no caption.
	/// </summary>
	[Test]
	public void AutoLockRemaining_Without_A_Countdown_Is_Empty()
	{
		// Act
		object? result = Convert(AppConverters.AutoLockRemaining, null);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="AppConverters.HotkeysToToolTip" />: the tip of a file with hotkeys ends with their keys.
	/// </summary>
	[Test]
	public void HotkeysToToolTip_Lists_The_Keys_Of_The_Hotkeys()
	{
		// Arrange
		HotkeyDto[] hotkeys = [.. HotkeyFactory.CreateHotkeyDtos(3)];

		// Act
		object? result = Convert(AppConverters.HotkeysToToolTip, hotkeys);

		// Assert
		result
			.Should()
			.BeOfType<string>()
			.Which
			.Should()
			.EndWith(hotkeys.ToKeyStrokes().ToArray().GetHotkeysPresentation());
	}

	/// <summary>
	/// <see cref="AppConverters.HotkeysToToolTip" />: a file without hotkeys has no tip.
	/// </summary>
	[Test]
	public void HotkeysToToolTip_Without_Hotkeys_Is_Empty()
	{
		// Act
		object? result = Convert(AppConverters.HotkeysToToolTip, []);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="AppConverters.LineEndingToCaption" />: line endings of one style are named after their system and characters.
	/// </summary>
	[TestCase(LineEnding.CrLf, "Windows (CR LF)")]
	[TestCase(LineEnding.Lf, "Unix (LF)")]
	[TestCase(LineEnding.Cr, "Macintosh (CR)")]
	public void LineEndingToCaption_Names_The_Line_Break(LineEnding ending, string expected)
	{
		// Act
		object? result = Convert(AppConverters.LineEndingToCaption, ending);

		// Assert
		result
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="AppConverters.LineEndingToCaption" />: a document without line breaks has no caption.
	/// </summary>
	[Test]
	public void LineEndingToCaption_Without_Line_Breaks_Is_Empty()
	{
		// Act
		object? result = Convert(AppConverters.LineEndingToCaption, LineEnding.None);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="AppConverters.MaterialDesignColorToBrush" />: a primary color is painted with its swatch.
	/// </summary>
	[Test]
	public void MaterialDesignColorToBrush_Paints_A_Primary_Color_With_Its_Swatch()
	{
		// Act
		object? result = Convert(AppConverters.MaterialDesignColorToBrush, ThemePrimaryColor.Teal);

		// Assert
		result
			.Should()
			.BeAssignableTo<ISolidColorBrush>()
			.Which
			.Color
			.Should()
			.Be(Color.Parse("#009688"));
	}

	/// <summary>
	/// <see cref="AppConverters.MaterialDesignColorToBrush" />: an accent color is painted with its swatch.
	/// </summary>
	[Test]
	public void MaterialDesignColorToBrush_Paints_A_Secondary_Color_With_Its_Swatch()
	{
		// Act
		object? result = Convert(AppConverters.MaterialDesignColorToBrush, ThemeSecondaryColor.Amber);

		// Assert
		result
			.Should()
			.BeAssignableTo<ISolidColorBrush>()
			.Which
			.Color
			.Should()
			.Be(Color.Parse("#FFAB00"));
	}

	/// <summary>
	/// <see cref="AppConverters.NumberCaption" />: puts the number, with its digits grouped, into the format given as the parameter.
	/// </summary>
	[Test]
	public void NumberCaption_Groups_The_Digits()
	{
		// Act
		object? result = Convert(AppConverters.NumberCaption, 1234567, "[{0}]");

		// Assert
		result
			.Should()
			.Be($"[{1234567.ToString("N0", CultureInfo.CurrentCulture)}]");
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
	/// <see cref="AppConverters.PngToPreview" />: the image is decoded at the width given as the parameter, whatever its own
	/// size.
	/// </summary>
	[AvaloniaTest]
	public void PngToPreview_Decodes_The_Png_At_The_Width_Given()
	{
		// Act
		Bitmap? result = (Bitmap?)Convert(AppConverters.PngToPreview, ImageFactory.CreatePng(), 48.0);

		// Assert
		result!.PixelSize.Width
			.Should()
			.Be(48);
	}

	/// <summary>
	/// <see cref="AppConverters.PngToPreview" />: empty bytes give no image.
	/// </summary>
	[Test]
	public void PngToPreview_Without_A_Png_Is_Empty()
	{
		// Act
		object? result = Convert(AppConverters.PngToPreview, [], 48.0);

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
	/// <see cref="AppConverters.SelectionOrPosition" />: while nothing is selected, shows the caret position counted from one.
	/// </summary>
	[Test]
	public void SelectionOrPosition_Shows_The_Position_Counted_From_One()
	{
		// Arrange
		DocumentStatus status = default(DocumentStatus) with
		{
			CaretOffset = 776
		};

		// Act
		object? result = Convert(AppConverters.SelectionOrPosition, status);

		// Assert
		result
			.Should()
			.BeOfType<string>()
			.Which
			.Should()
			.Contain("777");
	}

	/// <summary>
	/// <see cref="AppConverters.SelectionOrPosition" />: a selection shows its characters and lines instead of the caret position.
	/// </summary>
	[Test]
	public void SelectionOrPosition_Shows_The_Selection_Instead_Of_The_Position()
	{
		// Arrange
		DocumentStatus status = default(DocumentStatus) with
		{
			CaretOffset = 776,
			SelectionLength = 128,
			SelectionLineCount = 45
		};

		// Act
		object? result = Convert(AppConverters.SelectionOrPosition, status);

		// Assert
		result
			.Should()
			.BeOfType<string>()
			.Which
			.Should()
			.Contain("128")
			.And
			.Contain("45")
			.And
			.NotContain("777");
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
	/// <see cref="AppConverters.SyntaxLanguageToName" />: a language takes the name its grammar gives it.
	/// </summary>
	[Test]
	public void SyntaxLanguageToName_Gives_The_Name_Of_The_Grammar()
	{
		// Act
		object? result = Convert(AppConverters.SyntaxLanguageToName, "powershell");

		// Assert
		result
			.Should()
			.Be("PowerShell");
	}

	/// <summary>
	/// <see cref="AppConverters.SyntaxLanguageToName" />: a language without a grammar is named as plain text.
	/// </summary>
	[Test]
	[TestCase("unknown")]
	[TestCase(FileEditorState.PlainTextLanguage)]
	public void SyntaxLanguageToName_Names_A_Language_Without_A_Grammar_As_Plain_Text(string language)
	{
		// Act
		object? result = Convert(AppConverters.SyntaxLanguageToName, language);

		// Assert
		result
			.Should()
			.NotBeNull()
			.And
			.Be(Convert(AppConverters.SyntaxLanguageToName, null));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Runs a converter over the hotkeys of a file and their count, as the binding hands them over.
	/// </summary>
	private static object? Convert(IMultiValueConverter converter, HotkeyDto[] hotkeys)
	{
		return converter.Convert(
			[hotkeys, hotkeys.Length],
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
	/// Runs a converter over an auto-lock delay in minutes.
	/// </summary>
	private static object? Convert(FuncValueConverter<int, string> converter, int minutes)
	{
		return converter.Convert(
			minutes,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over the time left of a countdown.
	/// </summary>
	private static object? Convert(IValueConverter converter, TimeSpan? remaining)
	{
		return converter.Convert(
			remaining,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over the line endings of a document.
	/// </summary>
	private static object? Convert(FuncValueConverter<LineEnding, string?> converter, LineEnding ending)
	{
		return converter.Convert(
			ending,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over a color of the theme.
	/// </summary>
	private static object? Convert(FuncValueConverter<object?, IBrush?> converter, object color)
	{
		return converter.Convert(
			color,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over a number with the format given as the parameter.
	/// </summary>
	private static object? Convert(FuncValueConverter<int, string, string> converter, int number, string format)
	{
		return converter.Convert(
			number,
			typeof(object),
			format,
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
	/// Runs a converter over the bytes of a PNG with the width given as the parameter.
	/// </summary>
	private static object? Convert(FuncValueConverter<byte[]?, double, Bitmap?> converter, byte[]? png, double width)
	{
		return converter.Convert(
			png,
			typeof(object),
			width,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over the status of a document.
	/// </summary>
	private static object? Convert(FuncValueConverter<DocumentStatus, string> converter, DocumentStatus status)
	{
		return converter.Convert(
			status,
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
	/// Runs a converter over a language of the syntax highlighting.
	/// </summary>

	private static object? Convert(FuncValueConverter<string?, string> converter, string? language)
	{
		return converter.Convert(
			language,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}
	#endregion
}
