using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using INamed = DataOrganizer.Interfaces.Hierarchy.INamed;

namespace DataOrganizer.Converters;

/// <summary>
/// Stateless one-way value converters for any view, expressed as lambdas, referenced via x:Static.
/// </summary>
internal static class AppConverters
{
	#region Data
	/// <summary>
	/// Material vertical scrollbar thickness; the right gutter reserved while content overflows.
	/// </summary>
	private const double ScrollBarThickness = 10.0;
	#endregion

	#region Properties
	/// <inheritdoc cref="EnumToBoolConverter" />
	public static EnumToBoolConverter EnumToBool { get; } = new();

	/// <inheritdoc cref="FieldLabelAutomationNameConverter" />
	public static FieldLabelAutomationNameConverter FieldLabelAutomationName { get; } = new();

	/// <summary>
	/// Automation name of an item container: the name of its object when the object has one, otherwise unset.
	/// </summary>
	public static FuncValueConverter<object?, object?> NamedItemAutomationName { get; } =
		new(static x => x is INamed named ? named.Name : AvaloniaProperty.UnsetValue);

	/// <inheritdoc cref="PixelsToGridLengthConverter" />
	public static PixelsToGridLengthConverter PixelsToGridLength { get; } = new();

	/// <summary>
	/// Image of a PNG; <c>null</c> without one.
	/// </summary>
	public static FuncValueConverter<byte[]?, Bitmap?> PngToBitmap { get; } = new(GetBitmap);

	/// <summary>
	/// Right gutter for a <c>ScrollViewer</c>, reserved only while content overflows vertically.
	/// Inputs: [Extent.Height, Viewport.Height].
	/// </summary>
	public static FuncMultiValueConverter<double, Thickness> ScrollGutter { get; } = new(GetScrollGutter);

	/// <summary>
	/// New width of a control whose size changed; not a number without the change.
	/// </summary>
	public static FuncValueConverter<SizeChangedEventArgs?, double> SizeChangedToWidth { get; } =
		new(static x => x?.NewSize.Width ?? double.NaN);

	/// <inheritdoc cref="ToolTipAutomationNameConverter" />
	public static ToolTipAutomationNameConverter ToolTipAutomationName { get; } = new();

	/// <inheritdoc cref="WindowStateToBoolConverter" />
	public static WindowStateToBoolConverter WindowStateToBool { get; } = new();
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the image of <paramref name="png" />; <c>null</c> without one.
	/// </summary>
	private static Bitmap? GetBitmap(byte[]? png)
	{
		if (png is null)
		{
			return null;
		}

		using MemoryStream stream = new(png);

		return new Bitmap(stream);
	}

	/// <summary>
	/// Returns the right gutter of a scroll viewer: the width of the scroll bar while the content overflows.
	/// </summary>
	private static Thickness GetScrollGutter(IEnumerable<double> values)
	{
		if (values.ToArray() is [double extent, double viewport] && extent > viewport)
		{
			return new Thickness(0.0, 0.0, ScrollBarThickness, 0.0);
		}

		return default;
	}
	#endregion
}
