using SharpHook.Data;

namespace DataOrganizer.Messages;

/// <summary>
/// Notification raised by the global hook owner when a key is released.
/// </summary>
public sealed record GlobalKeyReleasedMessage(EventMask Mask, KeyCode Code);
