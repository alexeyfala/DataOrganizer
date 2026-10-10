using Avalonia.Controls;

namespace DataOrganizer.Extensions;

internal static class WindowExtensions
{
	#region Methods
	/// <summary>
	/// Brings a minimized window back to the normal state and activates it.
	/// </summary>
	public static void RestoreAndActivate(this Window target)
	{
		if (target.WindowState == WindowState.Minimized)
		{
			target.WindowState = WindowState.Normal;
		}

		target.Activate();
	}
	#endregion
}
