using Avalonia;
using DataOrganizer.UI;
using System;

namespace DataOrganizer.Desktop;

/// <summary>
/// Entry point of the application on Windows and Linux.
/// </summary>
public static class Program
{
	#region Methods
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	/// <summary>
	/// Starts the application with the classic desktop lifetime.
	/// </summary>
	[STAThread]
	public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
	#endregion

	#region Helpers
	// Avalonia configuration, don't remove; also used by visual designer.
	/// <summary>
	/// Builds the Avalonia application with its platform options.
	/// </summary>
	private static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder
			.Configure<App>()
			.UsePlatformDetect()
			.With(new X11PlatformOptions
			{
				// The application shows no native menu items, and exporting an empty menu only registers every window and
				// popup with the global menu of Linux, which leaves warnings when one closes at once.
				UseDBusMenu = false
			})
			.WithInterFont()
			.LogToTrace();
	}
	#endregion
}
