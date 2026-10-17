using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Clipboard;
using DataOrganizer.Services.Clipboard;
using DataOrganizer.Services.Runtime;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using NSubstitute;
using Repository.Interfaces.Database;
using Shared.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace DataOrganizer.UnitTests.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(CopyHistoryViewModel)}"" type")]
internal class CopyHistoryViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="CopyHistoryViewModel(IContentCopier, ICopyHistory, IMainWindowLauncher, IMessenger, ISessionHierarchy, ITaskExceptionHandler)" />:
	/// the list shows the files of the copy history that the tree holds, in the order of the history, with the chosen one
	/// selected.
	/// </summary>
	[Test]
	public void Constructor_Fills_The_List_From_The_Copy_History_And_Selects_The_Chosen_File()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(3)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([files[2].Id, Guid.NewGuid(), files[0].Id]));

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
		});

		SynchronizationContext.SetSynchronizationContext(null);

		// Act
		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		// Assert
		sut.Items
			.Should()
			.Equal(files[2], files[0]);

		sut.SelectedItem
			.Should()
			.Be(files[0]);
	}

	/// <summary>
	/// <see cref="FileListViewModelBase.CopyContentCommand" />: a file copied from the list keeps its place in it.
	/// </summary>
	[AvaloniaTest]
	public void CopyContentCommand_Keeps_The_Order_Of_The_List()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			IFileTextCopier fileTextCopier = Substitute.For<IFileTextCopier>();

			WorkSession session = new();

			dbAccess
				.ExistsAsync(files[1].Id, Arg.Any<CancellationToken>())
				.Returns(true);

			fileTextCopier
				.CopyAsync(files[1], Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(FileTextStatus.Text);

			session
				.Hierarchy
				.AddRange(files);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileTextCopier);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<ContentCopier>()
				.As<IContentCopier>();

			builder
				.RegisterType<CopyHistory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		CopyHistory copyHistory = mock.Create<CopyHistory>();

		// The history lists the first file above the second.
		copyHistory.Add(files[1], showAtOnce: false);

		copyHistory.Add(files[0], showAtOnce: false);

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		Dispatcher.UIThread.RunJobs();

		// Act
		sut
			.CopyContentCommand
			.Execute(files[1]);

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.Items
			.Should()
			.Equal(files);
	}

	/// <summary>
	/// <see cref="FileListViewModelBase.CopyContentCommand" />: a file copied from the list moves to the top of the copy
	/// history, which the list shows at its next showing.
	/// </summary>
	[AvaloniaTest]
	public void CopyContentCommand_Moves_The_File_To_The_Top_At_The_Next_Showing()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			IFileTextCopier fileTextCopier = Substitute.For<IFileTextCopier>();

			WorkSession session = new();

			dbAccess
				.ExistsAsync(files[1].Id, Arg.Any<CancellationToken>())
				.Returns(true);

			fileTextCopier
				.CopyAsync(files[1], Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(FileTextStatus.Text);

			session
				.Hierarchy
				.AddRange(files);

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(fileTextCopier);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<ContentCopier>()
				.As<IContentCopier>();

			builder
				.RegisterType<CopyHistory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		CopyHistory copyHistory = mock.Create<CopyHistory>();

		// The history lists the first file above the second.
		copyHistory.Add(files[1], showAtOnce: false);

		copyHistory.Add(files[0], showAtOnce: false);

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		Dispatcher.UIThread.RunJobs();

		// Act
		sut
			.CopyContentCommand
			.Execute(files[1]);

		Dispatcher.UIThread.RunJobs();

		// The list is shown again by a new view model.
		using ILifetimeScope nextShowing = mock.Container.BeginLifetimeScope();

		CopyHistoryViewModel shownAgain = nextShowing.Resolve<CopyHistoryViewModel>();

		Dispatcher.UIThread.RunJobs();

		// Assert
		shownAgain.Items
			.Should()
			.Equal(files[1], files[0]);
	}

	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: clears items and selection.
	/// </summary>
	[Test]
	public void Dispose_Clears_Properties()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(5)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([.. files.Select(x => x.Id)]));

			session
				.Hierarchy
				.AddRange(files);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		SynchronizationContext.SetSynchronizationContext(null);

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		sut.SelectedItem = files[0];

		// Act
		sut.Dispose();

		// Assert
		sut.IsEmpty
			.Should()
			.BeTrue();

		sut.SelectedItem
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: the copy history keeps the file chosen in the list, since a
	/// disposed list letting go of it is not a choice.
	/// </summary>
	[Test]
	public void Dispose_Keeps_The_Chosen_File_In_The_Copy_History()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([.. files.Select(x => x.Id)]));

			session
				.Hierarchy
				.AddRange(files);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		SynchronizationContext.SetSynchronizationContext(null);

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		sut.SelectedItem = files[1];

		// Act
		sut.Dispose();

		// Assert
		copyHistory.SelectedItemId
			.Should()
			.Be(files[1].Id);
	}

	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: a disposed list no longer follows the copy history.
	/// </summary>
	[Test]
	public void Dispose_Stops_Following_The_Copy_History()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([]));

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		SynchronizationContext.SetSynchronizationContext(null);

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		sut.Dispose();

		// Act
		mock
			.Create<IMessenger>()
			.Send(new CopyHistoryFileAddedMessage(ItemDtoFactory.CreateFileDto()));

		// Assert
		sut.IsEmpty
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="CopyHistoryViewModel.Receive(CopyHistoryClearedMessage)" />: a cleared copy history empties the list,
	/// its search and its choice.
	/// </summary>
	[Test]
	public void Receive_CopyHistoryCleared_Empties_The_List_Its_Search_And_Its_Choice()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(5)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([.. files.Select(x => x.Id)]));

			session
				.Hierarchy
				.AddRange(files);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();
		});

		SynchronizationContext.SetSynchronizationContext(null);

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		sut.SelectedItem = files[0];

		sut.HistorySearch = "SomeValue";

		// Act
		mock
			.Create<IMessenger>()
			.Send(new CopyHistoryClearedMessage());

		// Assert
		sut.IsEmpty
			.Should()
			.BeTrue();

		sut.HistorySearch
			.Should()
			.BeNull();

		sut.SelectedItem
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="CopyHistoryViewModel.Receive(CopyHistoryFileAddedMessage)" />: a file new to the list goes to its top.
	/// </summary>
	[Test]
	public void Receive_CopyHistoryFileAdded_Inserts_A_New_File_At_The_Top()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(3)];

		FileDto copied = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([.. files.Select(x => x.Id)]));

			session
				.Hierarchy
				.AddRange(files);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();
		});

		SynchronizationContext.SetSynchronizationContext(null);

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		// Act
		mock
			.Create<IMessenger>()
			.Send(new CopyHistoryFileAddedMessage(copied));

		// Assert
		sut.Items
			.Should()
			.Equal(copied, files[0], files[1], files[2]);
	}

	/// <summary>
	/// <see cref="CopyHistoryViewModel.Receive(CopyHistoryFileAddedMessage)" />: a file already in the list moves to its
	/// top without a second entry.
	/// </summary>
	[Test]
	public void Receive_CopyHistoryFileAdded_Moves_A_Listed_File_To_The_Top_Once()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(3)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([.. files.Select(x => x.Id)]));

			session
				.Hierarchy
				.AddRange(files);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();
		});

		SynchronizationContext.SetSynchronizationContext(null);

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		// Act
		mock
			.Create<IMessenger>()
			.Send(new CopyHistoryFileAddedMessage(files[2]));

		// Assert
		sut.Items
			.Should()
			.Equal(files[2], files[0], files[1]);
	}

	/// <summary>
	/// <see cref="CopyHistoryViewModel.Receive(CopyHistoryFileRemovedMessage)" />: a file that is not in the list leaves
	/// it as it is.
	/// </summary>
	[Test]
	public void Receive_CopyHistoryFileRemoved_Keeps_The_List_Without_The_File()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(3)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([.. files.Select(x => x.Id)]));

			session
				.Hierarchy
				.AddRange(files);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();
		});

		SynchronizationContext.SetSynchronizationContext(null);

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		// Act
		mock
			.Create<IMessenger>()
			.Send(new CopyHistoryFileRemovedMessage(ItemDtoFactory.CreateFileDto()));

		// Assert
		sut.Items
			.Should()
			.Equal(files);
	}

	/// <summary>
	/// <see cref="CopyHistoryViewModel.Receive(CopyHistoryFileRemovedMessage)" />: a file that leaves the copy history
	/// leaves the list.
	/// </summary>
	[Test]
	public void Receive_CopyHistoryFileRemoved_Removes_The_File()
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(3)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([.. files.Select(x => x.Id)]));

			session
				.Hierarchy
				.AddRange(files);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();

			builder
				.RegisterType<WeakReferenceMessenger>()
				.As<IMessenger>()
				.SingleInstance();
		});

		SynchronizationContext.SetSynchronizationContext(null);

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		// Act
		mock
			.Create<IMessenger>()
			.Send(new CopyHistoryFileRemovedMessage(files[1]));

		// Assert
		sut.Items
			.Should()
			.Equal(files[0], files[2]);
	}

	/// <summary>
	/// <see cref="CopyHistoryViewModel.SelectedItem" />: the copy history keeps the file chosen in the list, or none.
	/// </summary>
	[Test]
	public void SelectedItem_Is_Kept_In_The_Copy_History([Values] bool isChosen)
	{
		// Arrange
		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(2)];

		ICopyHistory copyHistory = Substitute.For<ICopyHistory>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			WorkSession session = new();

			copyHistory
				.ItemIds
				.Returns(new ReadOnlyObservableCollection<Guid>([.. files.Select(x => x.Id)]));

			session
				.Hierarchy
				.AddRange(files);

			builder.RegisterInstance(copyHistory);

			builder
				.RegisterInstance(session)
				.AsImplementedInterfaces();
		});

		SynchronizationContext.SetSynchronizationContext(null);

		CopyHistoryViewModel sut = mock.Create<CopyHistoryViewModel>();

		sut.SelectedItem = files[1];

		// Act
		sut.SelectedItem = isChosen ? files[0] : null;

		// Assert
		copyHistory.SelectedItemId
			.Should()
			.Be(isChosen ? files[0].Id : Guid.Empty);
	}
	#endregion
}
