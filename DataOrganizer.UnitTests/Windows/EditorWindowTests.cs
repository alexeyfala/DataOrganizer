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
using Shared.Common;
using System.Linq;

namespace DataOrganizer.UnitTests.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(EditorWindow)}"" type")]
internal class EditorWindowTests
{
	#region Methods
	/// <summary>
	/// <see cref="ViewModelBase.ShowNotepadCommand" />: a click on the notepad button of the title bar opens the notepad
	/// for the window.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_The_Notepad_Button_Shows_The_Notepad()
	{
		// Arrange
		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(viewLauncher));

		EditorWindow sut = new(mock.Create<EditorViewModel>());

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
	/// <see cref="EditorWindow()" />: the separator and the seeding items at the end of the menu are shown only in a debug
	/// build.
	/// </summary>
	[AvaloniaTest]
	public void Constructor_Shows_The_Seeding_Items_Only_In_A_Debug_Build()
	{
		// Act
		EditorWindow sut = new();

		// Assert
		Control[] items =
		[
			sut.SeedingSeparator,
			sut.SampleSeeding,
			sut.LargeSampleSeeding,
			sut.ClipboardHistorySeeding
		];

		// The failure names the elements: in an optimized build the message can quote another assertion.
		items.Where(x => x.IsVisible != AppInfo.IsDebug).Select(x => x.Name)
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ViewModelBase.CenterNotepadCommand" />: a double click on the notepad button of the title bar brings the
	/// notepad to the screen of the window.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_On_The_Notepad_Button_Centers_The_Notepad()
	{
		// Arrange
		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(viewLauncher));

		EditorWindow sut = new(mock.Create<EditorViewModel>());

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
