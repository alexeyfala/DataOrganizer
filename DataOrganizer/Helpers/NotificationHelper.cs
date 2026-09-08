using System;

namespace DataOrganizer.Helpers;

/// <summary>
/// Single source of truth for the values shared by the ways the application speaks to the user.
/// </summary>
internal static class NotificationHelper
{
	#region Data
	/// <summary>
	/// Time a message stays on the screen.
	/// </summary>
	public static readonly TimeSpan MessageDuration = TimeSpan.FromSeconds(4.0);
	#endregion
}
