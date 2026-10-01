using SharpHook.Data;

namespace DataOrganizer.Extensions;

internal static class EventMaskExtensions
{
	#region Methods
	/// <summary>
	/// Returns the modifier keys of <paramref name="source"/>, without the lock states, the mouse buttons and the flags of
	/// the event.
	/// </summary>
	public static EventMask ToModifiers(this EventMask source) => source & (EventMask.Shift | EventMask.Ctrl | EventMask.Alt | EventMask.Meta);
	#endregion
}
