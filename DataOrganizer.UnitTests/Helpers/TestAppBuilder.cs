using Avalonia;

namespace DataOrganizer.UnitTests.Helpers;

/// <summary>
/// Required class for testing Avalonia application<br />
/// <see href="https://docs.avaloniaui.net/docs/concepts/headless/headless-nunit" />.
/// </summary>
internal class TestAppBuilder
{
	#region Methods
	public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();
	#endregion
}
