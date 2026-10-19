using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using AwesomeAssertions;
using DataOrganizer.Converters;
using System.Globalization;

namespace DataOrganizer.UnitTests.Converters;

[TestFixture(Description = $@"Tests of ""{nameof(AppConverters)}"" type")]
internal class AppConvertersTests
{
	#region Methods
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
	#endregion

	#region Helpers
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
	#endregion
}
