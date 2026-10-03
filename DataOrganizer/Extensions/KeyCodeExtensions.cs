using SharpHook.Data;

namespace DataOrganizer.Extensions;

internal static class KeyCodeExtensions
{
	#region Methods
	/// <summary>
	/// <c>True</c> for a modifier key or a lock key, which takes part in a hotkey only through the mask of the keys pressed
	/// with it.
	/// </summary>
	public static bool IsModifierOrLock(this KeyCode code) => code switch
	{
		KeyCode.VcCapsLock => true,
		KeyCode.VcScrollLock => true,
		KeyCode.VcNumLock => true,
		KeyCode.VcLeftShift => true,
		KeyCode.VcRightShift => true,
		KeyCode.VcLeftControl => true,
		KeyCode.VcRightControl => true,
		KeyCode.VcLeftAlt => true,
		KeyCode.VcRightAlt => true,
		KeyCode.VcLeftMeta => true,
		KeyCode.VcRightMeta => true,
		_ => false
	};
	#endregion
}
