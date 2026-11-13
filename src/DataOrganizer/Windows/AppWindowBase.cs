using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace DataOrganizer.Windows;

/// <summary>
/// Window of the application whose platform window takes the wanted size at once until the window is shown, and which
/// UI Automation names by its automation name before its title.
/// </summary>
/// <remarks>
/// On Windows a window placed before its first show near the edge of a screen next to one with another scaling opened a
/// quarter visible: Avalonia took the scaling from the default size of the hidden platform window.
/// </remarks>
public abstract class AppWindowBase : Window
{
	#region Methods
	/// <inheritdoc />
	protected override AutomationPeer OnCreateAutomationPeer() => new AppWindowAutomationPeer(this);

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (IsVisible
			|| (change.Property != WidthProperty && change.Property != HeightProperty)
			|| double.IsNaN(Width)
			|| double.IsNaN(Height))
		{
			return;
		}

		// On Windows a move takes the scaling of the screen under the platform window, which keeps its default size until the
		// first show otherwise.
		ClientSize = new Size(Width, Height);
	}
	#endregion
}
