using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using Clipboard.Interfaces;
using Clipboard.Services;
using DataOrganizer.Behaviors.Input;
using DataOrganizer.Services.Runtime;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using FileContents.Enums;
using FileContents.Interfaces;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
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
		window.Click(button);

		await Dispatcher.UIThread.RunJobsUntilAsync(() => fileTextCopier.ReceivedCalls().Any());

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
		window.Click(button);

		await Dispatcher.UIThread.RunJobsUntilAsync(() => !ReferenceEquals(item.Background, background));

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
		window.Click(button);

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
		window.MouseMove(window.CenterOf(icon));

		await Dispatcher.UIThread.RunJobsUntilAsync(() => ToolTip.GetTip(icon) is not null);

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
	/// Returns the copy button of a file in the list.
	/// </summary>
	private static Button FindCopyButton(Window window, FileListViewModelBase viewModel, FileDto file)
	{
		return window
			.GetVisualDescendants()
			.OfType<Button>()
			.First(x => x.DataContext == file && x.Command == viewModel.CopyContentCommand);
	}
	#endregion
}
