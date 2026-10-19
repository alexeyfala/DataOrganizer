using Avalonia.Controls;
using System;

namespace DataOrganizer.Interfaces.Views;

/// <summary>
/// Finds the open windows of the desktop application.
/// </summary>
public interface IWindowFinder
{
	#region Methods
	/// <summary>
	/// Finds the main window: the editor or the favorites window.
	/// </summary>
	Window? FindMainWindow();

	/// <summary>
	/// Finds the first open window of type <typeparamref name="T" /> that meets <paramref name="condition" />.
	/// </summary>
	T? FindWindow<T>(Predicate<T>? condition = null) where T : Window;
	#endregion
}
