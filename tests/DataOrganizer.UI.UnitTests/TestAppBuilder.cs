using Avalonia;

namespace DataOrganizer.UI.UnitTests;

/// <summary>
/// Required class for testing Avalonia application<br />
/// <see href="https://docs.avaloniaui.net/docs/concepts/headless/headless-nunit" />.
/// </summary>
internal static class TestAppBuilder
{
	#region Methods
	/// <summary>
	/// Builds the application the headless tests run in.
	/// </summary>
	public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();
	#endregion
}
