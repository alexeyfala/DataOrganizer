namespace DataOrganizer.Helpers.Views;

/// <summary>
/// Keeps a navigation column narrower than the view that holds it.
/// </summary>
internal static class NavigationColumnHelper
{
	#region Data
	/// <summary>
	/// Space left beside the column when the view narrows.
	/// </summary>
	private const double Margin = 20.0;
	#endregion

	#region Methods
	/// <summary>
	/// Returns the column width that fits <paramref name="viewWidth" />, or <c>null</c> when the column keeps its width.
	/// </summary>
	public static double? FitWidth(double columnWidth, double viewWidth)
	{
		if (double.IsNaN(viewWidth) || viewWidth > columnWidth + Margin || viewWidth < Margin)
		{
			return null;
		}

		return viewWidth - Margin;
	}
	#endregion
}
