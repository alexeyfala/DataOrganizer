using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.UnitTests.Factories;
using System;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards that a disabled button of the app shows its tip, as an enabled one does")]
internal class DisabledButtonTipTests
{
	#region Methods
	/// <summary>
	/// A disabled button, a toggle button as well, opens its tip under the pointer.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Over_A_Disabled_Button_Opens_Its_Tip([Values(typeof(Button), typeof(ToggleButton))] Type kind)
	{
		// Arrange
		Button button = (Button)Activator.CreateInstance(kind)!;

		button.Content = "Button";

		button.IsEnabled = false;

		ToolTip.SetTip(button, "Tip");

		// The tip opens at once rather than after the delay of the theme.
		ToolTip.SetShowDelay(button, 0);

		Window window = HeadlessWindow.Show(button);

		// Act
		window.MouseMove(window.CenterOf(button));

		Dispatcher.UIThread.RunJobs();

		// Assert
		ToolTip.GetIsOpen(button)
			.Should()
			.BeTrue();
	}
	#endregion
}
