using Avalonia.Media.Imaging;

namespace DataOrganizer.DTO;

/// <summary>
/// Describes an application that can open a file: display name, executable path and
/// (optional) icon for UI presentation.
/// </summary>
public sealed class AssociatedAppInfo
{
	#region Properties
	/// <summary>
	/// Human-readable application name shown in the picker (e.g. "Notepad").
	/// </summary>
	public required string AppName { get; init; }


	/// <summary>
	/// Absolute path to the executable that opens the file.
	/// </summary>
	public required string AppPath { get; init; }

	/// <summary>
	/// Optional icon shown next to the application name; <c>null</c> when unavailable.
	/// </summary>
	public Bitmap? Icon { get; init; }
	#endregion
}
