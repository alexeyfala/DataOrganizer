using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.UI.Behaviors.Windows;
using DataOrganizer.UI.UnitTests.TestHelpers;
using System;

namespace DataOrganizer.UI.UnitTests.Behaviors.Windows;

/// <summary>
/// Tests of <see cref="TitleBarSystemMenuBehavior" /> type.
/// </summary>
[TestFixture]
internal sealed class TitleBarSystemMenuBehaviorTests
{
	#region Methods
	/// <summary>
	/// <see cref="TitleBarSystemMenuBehavior" />: a left press keeps the pointer captured by the title bar.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Left_Press_Keeps_The_Pointer_Captured()
	{
		// Arrange
		IPointer? pointer = null;

		Border titleBar = new()
		{
			Background = Brushes.Transparent,
			Height = 32
		};

		Interaction
			.GetBehaviors(titleBar)
			.Add(new TitleBarSystemMenuBehavior());

		titleBar.AddHandler(
			InputElement.PointerPressedEvent,
			(_, e) => pointer = e.Pointer,
			RoutingStrategies.Bubble,
			handledEventsToo: true);

		Window window = HeadlessWindow.Show(titleBar);

		// Act
		window.MouseDown(window.CenterOf(titleBar), MouseButton.Left);

		// Assert
		pointer!.Captured
			.Should()
			.BeSameAs(titleBar);
	}

	/// <summary>
	/// <see cref="TitleBarSystemMenuBehavior" />: a right press frees the pointer on Windows, so the release reaches the
	/// caption.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Right_Press_Frees_The_Pointer_On_Windows()
	{
		// Arrange
		IPointer? pointer = null;

		Border titleBar = new()
		{
			Background = Brushes.Transparent,
			Height = 32
		};

		Interaction
			.GetBehaviors(titleBar)
			.Add(new TitleBarSystemMenuBehavior());

		titleBar.AddHandler(
			InputElement.PointerPressedEvent,
			(_, e) => pointer = e.Pointer,
			RoutingStrategies.Bubble,
			handledEventsToo: true);

		Window window = HeadlessWindow.Show(titleBar);

		// Act
		window.MouseDown(window.CenterOf(titleBar), MouseButton.Right);

		// Assert
		pointer!.Captured
			.Should()
			.Be(OperatingSystem.IsWindows() ? null : titleBar);
	}
	#endregion
}
