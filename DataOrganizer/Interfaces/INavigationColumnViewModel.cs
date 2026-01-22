using Avalonia.Controls;

namespace DataOrganizer.Interfaces;

public interface INavigationColumnViewModel
{
	#region Properties
	/// <summary>
	/// Navigation column width.
	/// </summary>
	GridLength NavigationColumnWidth { get; set; }
	#endregion

	#region Methods
	/// <summary>
	/// Sets and validate the value for <see cref="NavigationColumnWidth" />.
	/// </summary>
	void SetNavigationColumnWidth(in double value)
	{
		const double increment = 20.0;

		if (double.IsNaN(value) || value > NavigationColumnWidth.Value + increment)
		{
			return;
		}

		double width = value - increment;

		if (width < default(double))
		{
			return;
		}

		NavigationColumnWidth = new GridLength(width);
	}
	#endregion
}
