using Shared.Common;

namespace DataOrganizer.Dto.Dialogs;

/// <summary>
/// Content of a notice shown in a window of its own.
/// </summary>
public sealed class NoticeParameters
{
	#region Properties
	/// <summary>
	/// Caption of the button that reveals <see cref="FilePath" />; without a caption there is no button.
	/// </summary>
	public string? ActionCaption { get; init; }

	/// <summary>
	/// Path shown under the message; without a path there is no line for it.
	/// </summary>
	public string? FilePath { get; init; }

	/// <summary>
	/// <c>True</c> keeps the notice above the windows of other applications.
	/// </summary>
	public bool IsTopmost { get; init; }

	/// <summary>
	/// Message.
	/// </summary>
	public required string Message { get; init; }

	/// <summary>
	/// Title of the window.
	/// </summary>
	public string Title { get; init; } = AppInfo.AppNameParted;
	#endregion
}
