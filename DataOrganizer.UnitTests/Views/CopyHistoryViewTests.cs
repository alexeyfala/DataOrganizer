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
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Input;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.Services.Runtime;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using Material.Icons.Avalonia;
using NSubstitute;
using Repository.Interfaces.Database;
using Shared.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Views;

[TestFixture(Description = $@"Tests of ""{nameof(CopyHistoryView)}"" type")]
internal class CopyHistoryViewTests
{
	#region Methods
	/// <summary>
	/// <see cref="FileListViewModelBase.CopyContentCommand" />: a click on the copy button of a file in the list copies the
	/// text of the file.
	/// </summary>
	[AvaloniaTest]
	public async Task Click_On_The_Copy_Button_Copies_The_Text_Of_The_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IFileTextCopier fileTextCopier = Substitute.For<IFileTextCopier>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			fileTextCopier
				.CopyAsync(file, Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(FileTextStatus.Text);

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([file.Id]));

			session
				.Hierarchy
				.Add(file);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileTextCopier);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<ContentCopier>()
				.As<IContentCopier>();

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		CopyHistoryViewModel viewModel = mock.Create<CopyHistoryViewModel>();

		CopyHistoryView view = new()
		{
			DataContext = viewModel
		};

		Window window = new()
		{
			Content = view,
			Height = 300.0,
			Width = 400.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		Button button = FindCopyButton(window, viewModel, file);

		// Act
		Click(window, button);

		await RunJobsUntilAsync(() => fileTextCopier.ReceivedCalls().Any());

		window.Close();

		// Assert
		await fileTextCopier
			.Received(1)
			.CopyAsync(file, Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="FileListViewModelBase.CopyContentCommand" />: a click on the copy button of a file in the list highlights
	/// the file there.
	/// </summary>
	[AvaloniaTest]
	public async Task Click_On_The_Copy_Button_Highlights_The_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			IFileTextCopier fileTextCopier = Substitute.For<IFileTextCopier>();

			dbAccess
				.ExistsAsync(file.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			fileTextCopier
				.CopyAsync(file, Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(FileTextStatus.Text);

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([file.Id]));

			session
				.Hierarchy
				.Add(file);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileTextCopier);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<ContentCopier>()
				.As<IContentCopier>();

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		CopyHistoryViewModel viewModel = mock.Create<CopyHistoryViewModel>();

		CopyHistoryView view = new()
		{
			DataContext = viewModel
		};

		Window window = new()
		{
			Content = view,
			Height = 300.0,
			Width = 400.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		ListBoxItem item = window
			.GetVisualDescendants()
			.OfType<ListBoxItem>()
			.First(x => x.DataContext == file);

		IBrush? background = item.Background;

		Button button = FindCopyButton(window, viewModel, file);

		// Act
		Click(window, button);

		await RunJobsUntilAsync(() => !ReferenceEquals(item.Background, background));

		IBrush? highlight = item.Background;

		window.Close();

		// Assert
		highlight
			.Should()
			.NotBeSameAs(background);
	}

	/// <summary>
	/// <see cref="FileListViewModelBase.CopyContentCommand" />: a click on the copy button of a file in the list selects the
	/// file.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_The_Copy_Button_Selects_The_File()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([.. files.Select(x => x.Id)]));

			copyHistory
				.SelectedItemId
				.Returns(files[0].Id);

			session
				.Hierarchy
				.AddRange(files);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		CopyHistoryViewModel viewModel = mock.Create<CopyHistoryViewModel>();

		CopyHistoryView view = new()
		{
			DataContext = viewModel
		};

		Window window = new()
		{
			Content = view,
			Height = 300.0,
			Width = 400.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		Button button = FindCopyButton(window, viewModel, files[1]);

		// Act
		Click(window, button);

		Dispatcher.UIThread.RunJobs();

		window.Close();

		// Assert
		viewModel.SelectedItem
			.Should()
			.BeSameAs(files[1]);
	}

	/// <summary>
	/// <see cref="FileListViewModelBase.CopyContentCommand" />: the copy button of a file open in the editor is disabled.
	/// </summary>
	[AvaloniaTest]
	public void Copy_Button_Of_An_Opened_File_Is_Disabled()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(isEditing: true);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([file.Id]));

			session
				.Hierarchy
				.Add(file);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		CopyHistoryViewModel viewModel = mock.Create<CopyHistoryViewModel>();

		CopyHistoryView view = new()
		{
			DataContext = viewModel
		};

		Window window = new()
		{
			Content = view,
			Height = 300.0,
			Width = 400.0
		};

		// Act
		window.Show();

		Dispatcher.UIThread.RunJobs();

		bool isEnabled = FindCopyButton(window, viewModel, file).IsEffectivelyEnabled;

		window.Close();

		// Assert
		isEnabled
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FilePreviewToolTipBehavior" />: the pointer resting on the eye icon of a file shows the start of its
	/// text in a tip.
	/// </summary>
	[AvaloniaTest]
	public async Task Pointer_On_The_Eye_Icon_Shows_The_Preview_Of_The_File()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		IFilePreviewReader reader = Substitute.For<IFilePreviewReader>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			reader
				.ReadPreviewAsync(file)
				.Returns("Hi");

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([file.Id]));

			session
				.Hierarchy
				.Add(file);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			// The running headless application, since Application cannot be substituted.
			builder.RegisterInstance(Application.Current!);
		});

		CopyHistoryViewModel viewModel = mock.Create<CopyHistoryViewModel>();

		CopyHistoryView view = new()
		{
			DataContext = viewModel
		};

		Window window = new()
		{
			Content = view,
			Height = 300.0,
			Width = 400.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		MaterialIcon icon = window
			.GetVisualDescendants()
			.OfType<MaterialIcon>()
			.First(x => x.DataContext == file && Interaction.GetBehaviors(x).OfType<FilePreviewToolTipBehavior>().Any());

		// The markup resolves the reader from the services of the running application, which a test has not.
		Interaction
			.GetBehaviors(icon)
			.OfType<FilePreviewToolTipBehavior>()
			.Single()
			.Reader = reader;

		// Act
		window.MouseMove(icon.TranslatePoint(new Rect(icon.Bounds.Size).Center, window)!.Value);

		await RunJobsUntilAsync(() => ToolTip.GetTip(icon) is not null);

		object? tip = ToolTip.GetTip(icon);

		ToolTip.SetIsOpen(icon, false);

		window.Close();

		// Assert
		tip
			.Should()
			.Be("Hi");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Presses and releases the left button of the mouse in the middle of a button.
	/// </summary>
	private static void Click(Window window, Button button)
	{
		Point point = button.TranslatePoint(new Rect(button.Bounds.Size).Center, window)!.Value;

		window.MouseDown(point, MouseButton.Left);

		window.MouseUp(point, MouseButton.Left);
	}

	/// <summary>
	/// Returns the copy button of a file in the list.
	/// </summary>
	private static Button FindCopyButton(Window window, FileListViewModelBase viewModel, FileDto file)
	{
		return window
			.GetVisualDescendants()
			.OfType<Button>()
			.First(x => x.DataContext == file && x.Command == viewModel.CopyContentCommand);
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
