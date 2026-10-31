using Avalonia.Controls;
using Avalonia.Threading;

namespace DataOrganizer.UnitTests;

/// <summary>
/// Shows controls under test in a window of the headless platform.
/// </summary>
internal static class HeadlessWindow
{
	#region Methods
	/// <summary>
	/// Shows the content in a window of a fixed size and lets the layout settle.
	/// </summary>
	public static Window Show(
		Control content,
		double width = 800.0,
		double height = 600.0)
	{
		Window window = new()
		{
			Content = content,
			Height = height,
			Width = width
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		return window;
	}
	#endregion
}
