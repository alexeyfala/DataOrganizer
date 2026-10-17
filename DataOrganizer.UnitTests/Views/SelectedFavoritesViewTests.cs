using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Favorites;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using NSubstitute;
using Repository.Interfaces.Database;
using Shared.Extensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(SelectedFavoritesView)}"" type")]
internal class SelectedFavoritesViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileListViewModelBase.CopyContentCommand" />: a click on the copy button of a favorite highlights it in the
	/// list.
	/// </summary>
	[AvaloniaTest]
	public async Task Click_On_The_Copy_Button_Highlights_The_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		FavoriteCategory category = FavoriteFactory.CreateFavoriteCategory();

		category.Children.Add(file);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			IFileTextCopier fileTextCopier = Substitute.For<IFileTextCopier>();

			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			fileTextCopier
				.CopyAsync(file, Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(FileTextStatus.Text);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileTextCopier);

			builder
				.RegisterType<ContentCopier>()
				.As<IContentCopier>();

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		SelectedFavoritesViewModel viewModel = mock.Create<SelectedFavoritesViewModel>();

		viewModel.Initialize(
			navigationColumnWidth: 150.0,
			selectedCategoryId: category.Id,
			categories: [category],
			orderedCategoryIds: [category.Id],
			selectedPairs: []);

		SelectedFavoritesView view = new()
		{
			DataContext = viewModel
		};

		Window window = new()
		{
			Content = view,
			Height = 300.0,
			Width = 600.0
		};

		window.Show();

		await RunJobsUntilAsync(() => FindItem(window, file) is not null);

		ListBoxItem item = FindItem(window, file)!;

		IBrush? background = item.Background;

		Button button = window
			.GetVisualDescendants()
			.OfType<Button>()
			.First(x => x.DataContext == file && x.Command == viewModel.CopyContentCommand);

		Point point = button
			.TranslatePoint(new Rect(button.Bounds.Size).Center, window)!
			.Value;

		// Act
		window.MouseDown(point, MouseButton.Left);

		window.MouseUp(point, MouseButton.Left);

		await RunJobsUntilAsync(() => !ReferenceEquals(item.Background, background));

		IBrush? highlight = item.Background;

		window.Close();

		// Assert
		highlight
			.Should()
			.NotBeSameAs(background);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the item of the favorites list that shows a file; <c>null</c> while the list shows none.
	/// </summary>
	private static ListBoxItem? FindItem(Window window, FileDto file)
	{
		return window
			.GetVisualDescendants()
			.OfType<ListBoxItem>()
			.FirstOrDefault(x => x.DataContext == file);
	}

	/// <summary>
	/// Runs the jobs of the UI thread until <paramref name="condition" /> holds or the wait runs out.
	/// </summary>
	private static async Task RunJobsUntilAsync(Func<bool> condition)
	{
		Func<bool> isMet = () =>
		{
			Dispatcher.UIThread.RunJobs();

			return condition();
		};

		await isMet.WaitAsync(millisecondsDelay: 10, maxRepeats: 300);
	}
	#endregion
}
