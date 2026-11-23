using Avalonia;
using DataOrganizer.UI;
using System;

namespace DataOrganizer.MacOS;

// ico To icns Converter: https://pcpartguide.com/ico-to-icns
/// <summary>
/// Entry point of the application on macOS.
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
	private static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder
			.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace()
			.With(new MacOSPlatformOptions
			{
				//ShowInDock = false,
				//DisableDefaultApplicationMenuItems = true
			});
	}
	#endregion
}
