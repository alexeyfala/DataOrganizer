using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Windows;
using NSubstitute;

namespace DataOrganizer.UnitTests.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(NotepadWindow)}"" type")]
internal class NotepadWindowTests
{
	#region Methods
	/// <summary>
	/// <see cref="NotepadViewModel.ActivateMainWindowCommand" />: a click on the home button of the title bar activates
	/// the main window.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_The_Home_Button_Activates_The_Main_Window()
	{
		// Arrange
		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(viewLauncher));

		NotepadWindow sut = new(mock.Create<NotepadViewModel>());

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Point point = Center(sut, sut.HomeButton);

		// Act
		Click(sut, point);

		// Assert
		viewLauncher
			.Received(1)
			.ActivateMainWindow();
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CenterMainWindowCommand" />: a double click on the home button of the title bar brings
	/// the main window to the screen of the notepad.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_On_The_Home_Button_Centers_The_Main_Window()
	{
		// Arrange
		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(viewLauncher));

		NotepadWindow sut = new(mock.Create<NotepadViewModel>());

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Point point = Center(sut, sut.HomeButton);

		// Act
		Click(sut, point);

		Click(sut, point);

		// Assert
		viewLauncher
			.Received(1)
			.CenterMainWindow(sut);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the point in the middle of the control.
	/// </summary>
	private static Point Center(Visual root, Visual target)
	{
		return target.TranslatePoint(
			new(
				target.Bounds.Width / 2.0,
				target.Bounds.Height / 2.0),
			root) ?? default;
	}

	/// <summary>
	/// Presses and releases the left button of the mouse at a point of a window.
	/// </summary>
	private static void Click(Window window, Point point)
	{
		window.MouseDown(point, MouseButton.Left);

		window.MouseUp(point, MouseButton.Left);
	}
	#endregion
}
