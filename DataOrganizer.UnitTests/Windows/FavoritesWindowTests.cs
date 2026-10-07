using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Windows;
using NSubstitute;

namespace DataOrganizer.UnitTests.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(FavoritesWindow)}"" type")]
internal class FavoritesWindowTests
{
	#region Methods
	/// <summary>
	/// <see cref="ViewModelBase.ShowNotepadCommand" />: a click on the notepad button of the header opens the notepad for
	/// the window.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_The_Notepad_Button_Shows_The_Notepad()
	{
		// Arrange
		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(viewLauncher));

		FavoritesWindow sut = new(mock.Create<FavoritesViewModel>());

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Point point = Center(sut, sut.NotepadButton);

		// Act
		Click(sut, point);

		// Assert
		viewLauncher
			.Received(1)
			.ShowNotepadWindow(sut);
	}

	/// <summary>
	/// <see cref="ViewModelBase.CenterNotepadCommand" />: a double click on the notepad button of the header brings the
	/// notepad to the screen of the window.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_On_The_Notepad_Button_Centers_The_Notepad()
	{
		// Arrange
		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(viewLauncher));

		FavoritesWindow sut = new(mock.Create<FavoritesViewModel>());

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Point point = Center(sut, sut.NotepadButton);

		// Act
		Click(sut, point);

		Click(sut, point);

		// Assert
		viewLauncher
			.Received(1)
			.CenterNotepadWindow(sut);
	}

	/// <summary>
	/// <see cref="FavoritesWindow" />: a double click on the notepad button stays there and does not reach the header,
	/// whose double click switches to the editor.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_On_The_Notepad_Button_Keeps_The_Window_Open()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		FavoritesWindow sut = new(mock.Create<FavoritesViewModel>());

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Point point = Center(sut, sut.NotepadButton);

		// Act
		Click(sut, point);

		Click(sut, point);

		// Assert
		sut.IsVisible
			.Should()
			.BeTrue();
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
