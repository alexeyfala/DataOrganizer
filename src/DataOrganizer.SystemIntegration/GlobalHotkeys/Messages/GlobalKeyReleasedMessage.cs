using SharpHook.Data;

namespace DataOrganizer.SystemIntegration.GlobalHotkeys.Messages;

/// <summary>
/// Notification raised by the global hook owner when a key is released.
/// </summary>
/// <param name="Mask">Modifier keys and mouse buttons held when the key was released.</param>
/// <param name="Code">Code of the released key.</param>
public sealed record GlobalKeyReleasedMessage(EventMask Mask, KeyCode Code);
