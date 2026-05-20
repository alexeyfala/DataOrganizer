using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace DataOrganizer.Helpers;

/// <summary>
/// Factory for the placeholder control shown when no matching template is found.
/// </summary>
internal static class PlugControl
{
	#region Methods
	/// <summary>
	/// Returns the default placeholder control.
	/// </summary>
	public static Control Create(string? typeName) => new TextBlock
	{
		FontSize = 24.0,
		Foreground = Brushes.OrangeRed,
		HorizontalAlignment = HorizontalAlignment.Center,
		Text = "Not found view for: " + typeName,
		VerticalAlignment = VerticalAlignment.Center
	};
	#endregion
}
