using Avalonia.Controls;
using DataOrganizer.Windows;
using System;

namespace DataOrganizer.UnitTests.Fakes;

/// <summary>
/// Test-only plain window that counts as a main window of the application, as the editor or the favorites window does.
/// </summary>
internal sealed class TestMainWindow : Window, IMainWindow
{
	#region Properties
	/// <inheritdoc />
	public bool IsClosingToSwitch { get; set; }

	/// <inheritdoc />
	protected override Type StyleKeyOverride => typeof(Window);
	#endregion
}
