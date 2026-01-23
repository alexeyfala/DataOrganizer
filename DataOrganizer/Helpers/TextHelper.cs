using System.Text;

namespace DataOrganizer.Helpers;

public static class TextHelper
{
	#region Data
	/// <summary>
	/// Lorem ipsum ... .
	/// </summary>
	public static string LoremIpsum { get; } = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

	/// <inheritdoc cref="Encoding.UTF8" />
	public static Encoding Utf8Encoding { get; } = Encoding.UTF8;
	#endregion
}
