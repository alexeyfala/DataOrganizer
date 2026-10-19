using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Controls;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services.Runtime;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using Entities.Enums;
using NSubstitute;
using Shared.Common;
using Shared.Extensions;
using System.Linq;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(EditingFilesView)}"" type")]
internal class EditingFilesViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="EditingFilesViewModel.CloseTabCommand" />: the close button of a tab closes its file.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_The_Close_Button_Closes_The_File()
	{
		// Arrange
		// A kind with no editor keeps the content of the tab light.
		FileDto dto = ItemDtoFactory.CreateFileDto(kind: EntityKind.Folder);

		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel viewModel = mock.Create<EditingFilesViewModel>();

		viewModel.OpenInEditor(dto);

		Window window = new()
		{
			Content = new EditingFilesView
			{
				DataContext = viewModel
			},
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		Button closeButton = window
			.GetVisualDescendants()
			.OfType<DocumentTabControl>()
			.Single()
			.ContainerFromIndex(0)!
			.GetVisualDescendants()
			.OfType<Button>()
			.Single();

		Point point = closeButton.TranslatePoint(
			new(
				closeButton.Bounds.Width / 2.0,
				closeButton.Bounds.Height / 2.0),
			window) ?? default;

		// Act
		window.MouseDown(point, MouseButton.Left);

		window.MouseUp(point, MouseButton.Left);

		// Assert
		viewModel.Items
			.Should()
			.NotContain(dto);
	}

	/// <summary>
	/// <see cref="EditingFilesViewModel.CloseTabCommand" />: the view of a closed file leaves the cache once another tab
	/// has taken the place of its tab.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_The_Close_Button_Forgets_The_View_Once_Another_Tab_Is_Selected()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(count: 2)];

		IDataTemplate template = Substitute.For<IDataTemplate, IViewCache>();

		template
			.Match(Arg.Any<object?>())
			.Returns(x => x.Arg<object?>() is FileDto);

		template
			.Build(Arg.Any<object?>())
			.Returns(_ => new Border());

		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel viewModel = mock.Create<EditingFilesViewModel>();

		viewModel.OpenInEditor(files[0]);

		viewModel.OpenInEditor(files[1]);

		viewModel.SelectedIndex = 0;

		Window window = new()
		{
			Content = new EditingFilesView
			{
				DataContext = viewModel
			},
			Height = 600.0,
			Width = 800.0
		};

		// The application keeps the template among its own, which the tests do without.
		window
			.DataTemplates
			.Add(template);

		window.Show();

		Dispatcher.UIThread.RunJobs();

		DocumentTabControl tabs = window
			.GetVisualDescendants()
			.OfType<DocumentTabControl>()
			.Single();

		object? selected = null;

		((IViewCache)template)
			.When(x => x.Remove(files[0]))
			.Do(_ => selected = tabs.SelectedItem);

		Button closeButton = tabs
			.ContainerFromIndex(0)!
			.GetVisualDescendants()
			.OfType<Button>()
			.Single();

		Point point = closeButton.TranslatePoint(
			new(
				closeButton.Bounds.Width / 2.0,
				closeButton.Bounds.Height / 2.0),
			window) ?? default;

		// Act
		window.MouseDown(point, MouseButton.Left);

		window.MouseUp(point, MouseButton.Left);

		// Assert
		selected
			.Should()
			.BeSameAs(files[1]);
	}

	/// <summary>
	/// <see cref="EditingFilesViewModel.CloseEditor" />: a file closed after its window, as on the way to the favorites,
	/// leaves the cache with its view.
	/// </summary>
	[AvaloniaTest]
	public void CloseEditor_After_The_Window_Closes_Forgets_The_View_Of_The_File()
	{
		// Arrange
		FileDto dto = ItemDtoFactory.CreateFileDto();

		IDataTemplate template = Substitute.For<IDataTemplate, IViewCache>();

		template
			.Match(Arg.Any<object?>())
			.Returns(x => x.Arg<object?>() is FileDto);

		template
			.Build(Arg.Any<object?>())
			.Returns(_ => new Border());

		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel viewModel = mock.Create<EditingFilesViewModel>();

		viewModel.OpenInEditor(dto);

		Window window = new()
		{
			Content = new EditingFilesView
			{
				DataContext = viewModel
			},
			Height = 600.0,
			Width = 800.0
		};

		// The application keeps the template among its own, which the tests do without.
		window
			.DataTemplates
			.Add(template);

		window.Show();

		Dispatcher.UIThread.RunJobs();

		window.Close();

		// Act
		viewModel.CloseEditor(dto);

		// Assert
		((IViewCache)template)
			.Received(1)
			.Remove(dto);
	}

	/// <summary>
	/// <see cref="EditingFilesViewModel.PreviousFile" />: after the tabs come back, Ctrl+Tab goes to the tab of the
	/// previous file.
	/// </summary>
	[AvaloniaTest]
	public void CtrlTab_After_Restore_Goes_To_The_Previous_File()
	{
		// Arrange
		// A kind with no editor keeps the content of the tab light.
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(count: 3, kind: EntityKind.Folder)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			WorkSession session = new()
			{
				EditorTabs = new()
				{
					Files = files,
					PreviousFile = files[1],
					SelectedFile = files[2]
				}
			};

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		EditingFilesViewModel viewModel = mock.Create<EditingFilesViewModel>();

		Window window = new()
		{
			Content = new EditingFilesView
			{
				DataContext = viewModel
			},
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		// The tabs come back to the shown view, as when the editor window loads it.
		viewModel
			.RestoreTabsCommand
			.Execute(null);

		Dispatcher.UIThread.RunJobs();

		// Act
		window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Control);

		window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.Control);

		// Assert
		viewModel.SelectedIndex
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="EditingFilesViewModel.PreviousFile" />: a selected tab leaves the file of the tab selected before it as
	/// the previous one.
	/// </summary>
	[AvaloniaTest]
	public void PreviousFile_Follows_The_Selected_Tab()
	{
		// Arrange
		// A kind with no editor keeps the content of the tab light.
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(count: 2, kind: EntityKind.Folder)];

		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel viewModel = mock.Create<EditingFilesViewModel>();

		viewModel
			.Items
			.AddRange(files);

		Window window = new()
		{
			Content = new EditingFilesView
			{
				DataContext = viewModel
			},
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		viewModel.SelectedIndex = 1;

		// Assert
		viewModel.PreviousFile
			.Should()
			.BeSameAs(files[0]);
	}

	/// <summary>
	/// <see cref="ExplorerItemDtoBase.Name" />: a long name of a file is cut at the width of a tab header and shown whole in
	/// a tip.
	/// </summary>
	[AvaloniaTest]
	public void Tabs_Trim_A_Long_File_Name_With_A_Tip()
	{
		// Arrange
		// A kind with no editor keeps the content of the tab light.
		FileDto dto = ItemDtoFactory.CreateFileDto(kind: EntityKind.Folder);

		dto.Name = RandomString.Create(200);

		using AutoMock mock = AutoMock.GetLoose();

		EditingFilesViewModel viewModel = mock.Create<EditingFilesViewModel>();

		viewModel.OpenInEditor(dto);

		Window window = new()
		{
			Content = new EditingFilesView
			{
				DataContext = viewModel
			},
			Height = 600.0,
			Width = 800.0
		};

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert
		TextBlock header = window
			.GetVisualDescendants()
			.OfType<DocumentTabControl>()
			.Single()
			.ContainerFromIndex(0)!
			.GetVisualDescendants()
			.OfType<TextBlock>()
			.Single(x => x.Text == dto.Name);

		ToolTip.GetTip(header)
			.Should()
			.Be(dto.Name);
	}
	#endregion
}
