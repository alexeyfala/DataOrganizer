namespace Shared.Enums;

/// <summary>
/// Operating system the application runs on.
/// </summary>
public enum OperatingSystemKind
{
	/// <summary>
	/// The system could not be told apart from the ones below.
	/// </summary>
	Unknown,

	/// <summary>
	/// Microsoft Windows.
	/// </summary>
	Windows,

	/// <summary>
	/// A Linux distribution.
	/// </summary>
	Linux,

	/// <summary>
	/// Apple macOS.
	/// </summary>
	MacOS
}
