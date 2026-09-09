using System;

namespace DataOrganizer.Services;

/// <summary>
/// Timings and limits every message channel keeps to, so that a toast and a snackbar behave alike.
/// </summary>
internal static class MessageChannelOptions
{
	#region Data
	/// <summary>
	/// Number of messages kept waiting; the ones on top of that are dropped.
	/// </summary>
	internal const int MaxWaitingMessages = 10;

	/// <summary>
	/// Time a message stays on the screen.
	/// </summary>
	internal static readonly TimeSpan MessageDuration = TimeSpan.FromSeconds(4.0);

	/// <summary>
	/// Pause between a message leaving the screen and the next one taking its place.
	/// </summary>
	internal static readonly TimeSpan MessageGap = TimeSpan.FromSeconds(0.2);

	/// <summary>
	/// Interval between the checks for a message whose time is up.
	/// </summary>
	internal static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(0.25);
	#endregion
}
