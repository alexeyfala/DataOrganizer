using Avalonia;
using Avalonia.Media;

namespace DataOrganizer.UI.UnitTests.TestHelpers;

/// <summary>
/// What a visual paints, recorded for a test.
/// </summary>
internal static class VisualExtensions
{
	#region Methods
	/// <summary>
	/// Records what the visual paints.
	/// </summary>
	public static DrawingGroup Draw(this Visual visual)
	{
		DrawingGroup drawing = new();

		using (DrawingContext context = drawing.Open())
		{
			visual.Render(context);
		}

		return drawing;
	}
	#endregion
}
