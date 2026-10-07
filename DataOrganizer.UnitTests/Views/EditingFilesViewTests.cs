using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Controls;
using DataOrganizer.Dto.Entities;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using Entities.Enums;
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
	#endregion
}
