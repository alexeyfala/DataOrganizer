using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.Services.Runtime;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Views;
using DataOrganizer.Windows;
using NSubstitute;
using Shared.Common;
using System.Linq;

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
		ISecondaryWindowLauncher secondaryWindowLauncher = Substitute.For<ISecondaryWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(secondaryWindowLauncher);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesWindow sut = new(mock.Create<FavoritesViewModel>());

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Point point = Center(sut, sut.NotepadButton);

		// Act
		Click(sut, point);

		// Assert
		secondaryWindowLauncher
			.Received(1)
			.ShowNotepadWindow();
	}

	/// <summary>
	/// <see cref="FavoritesWindow" />: a double click on the fix switch stays there and does not reach the header, whose
	/// double click switches to the editor.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_On_The_Fix_Switch_Leaves_The_Editor_Alone()
	{
		// Arrange
		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(mainWindowLauncher);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesWindow sut = new(mock.Create<FavoritesViewModel>());

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Point point = Center(sut, sut.FixSwitch);

		// Act
		Click(sut, point);

		Click(sut, point);

		sut.Close();

		// Assert
		mainWindowLauncher
			.DidNotReceiveWithAnyArgs()
			.SwitchToEditor();
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.ShowEditorCommand" />: a double click on the header switches to the editor.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_On_The_Header_Switches_To_The_Editor()
	{
		// Arrange
		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(mainWindowLauncher);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesWindow sut = new(mock.Create<FavoritesViewModel>());

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		TextBlock title = sut
			.GetLogicalDescendants()
			.OfType<TextBlock>()
			.First(x => x.Text == AppInfo.AppDisplayName);

		Point point = Center(sut, title);

		// Act
		Click(sut, point);

		Click(sut, point);

		sut.Close();

		// Assert
		mainWindowLauncher
			.Received(1)
			.SwitchToEditor(default);
	}

	/// <summary>
	/// <see cref="ViewModelBase.CenterNotepadCommand" />: a double click on the notepad button of the header brings the
	/// notepad to the screen of the window.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_On_The_Notepad_Button_Centers_The_Notepad()
	{
		// Arrange
		ISecondaryWindowLauncher secondaryWindowLauncher = Substitute.For<ISecondaryWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(secondaryWindowLauncher);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesWindow sut = new(mock.Create<FavoritesViewModel>());

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Point point = Center(sut, sut.NotepadButton);

		// Act
		Click(sut, point);

		Click(sut, point);

		// Assert
		secondaryWindowLauncher
			.Received(1)
			.CenterNotepadWindow();
	}

	/// <summary>
	/// <see cref="FavoritesWindow" />: a double click on the notepad button stays there and does not reach the header,
	/// whose double click switches to the editor.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_On_The_Notepad_Button_Leaves_The_Editor_Alone()
	{
		// Arrange
		IMainWindowLauncher mainWindowLauncher = Substitute.For<IMainWindowLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(mainWindowLauncher);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesWindow sut = new(mock.Create<FavoritesViewModel>());

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Point point = Center(sut, sut.NotepadButton);

		// Act
		Click(sut, point);

		Click(sut, point);

		sut.Close();

		// Assert
		mainWindowLauncher
			.DidNotReceiveWithAnyArgs()
			.SwitchToEditor();
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.ShowCopyHistoryCommand" />: the popup shows the list of the copy history that the
	/// window made and not the favorites.
	/// </summary>
	[AvaloniaTest]
	public void ShowCopyHistoryCommand_Shows_The_List_Of_The_Window()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<CopyHistory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel viewModel = mock.Create<FavoritesViewModel>();

		FavoritesWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		viewModel
			.ShowCopyHistoryCommand
			.Execute(null);

		Dispatcher.UIThread.RunJobs();

		object?[] shown =
		[
			.. sut
				.GetLogicalDescendants()
				.OfType<Control>()
				.Where(x => x is CopyHistoryView or SelectedFavoritesView)
				.Select(x => x.DataContext)
		];

		CopyHistoryViewModel? list = viewModel.CopyHistoryList;

		sut.Close();

		// Assert
		shown
			.Should()
			.ContainSingle()
			.Which
			.Should()

			.NotBeNull()
			.And
			.BeSameAs(list);
	}

	/// <summary>
	/// <see cref="FavoritesViewModel.ShowFavorites" />: the popup shows the favorites that the window made.
	/// </summary>
	[AvaloniaTest]
	public void ShowFavorites_Shows_The_Favorites_Of_The_Window()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<ScopeViewFactory>()
				.AsImplementedInterfaces();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FavoritesViewModel viewModel = mock.Create<FavoritesViewModel>();

		FavoritesWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		viewModel.ShowFavorites();

		Dispatcher.UIThread.RunJobs();

		object? shown = sut
			.GetLogicalDescendants()
			.OfType<SelectedFavoritesView>()
			.Single()
			.DataContext;

		SelectedFavoritesViewModel? favorites = viewModel.SelectedFavorites;

		sut.Close();

		// Assert
		shown
			.Should()
			.NotBeNull()
			.And
			.BeSameAs(favorites);
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
