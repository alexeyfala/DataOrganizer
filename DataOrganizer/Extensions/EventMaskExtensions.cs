using SharpHook.Data;

namespace DataOrganizer.Extensions;

internal static class EventMaskExtensions
{
	#region Methods
	/// <summary>
	/// Removes flag <paramref name="toRemove"/> from <paramref name="source"/>.
	/// </summary>
	public static EventMask RemoveFlag(this EventMask source, EventMask toRemove) => source &= ~toRemove;
	#endregion
}
