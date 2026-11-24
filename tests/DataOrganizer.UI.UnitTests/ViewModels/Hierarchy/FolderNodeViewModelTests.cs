using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.FilteredLists.Dto;
using DataOrganizer.Hierarchy.UnitTests.TestHelpers.Factories;
using DataOrganizer.UI.ViewModels;
using DataOrganizer.UI.ViewModels.Hierarchy;
using DataOrganizer.Workflows.AppLifecycle.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace DataOrganizer.UI.UnitTests.ViewModels.Hierarchy;

[TestFixture(Description = $@"Tests of ""{nameof(FolderNodeViewModel)}"" type")]
internal sealed class FolderNodeViewModelTests
{
	#region Data
	/// <summary>
	/// Name filter that never changes, for rows whose shown objects the tests do not read.
	/// </summary>
	private static readonly IObservable<HierarchyFilter> UnusedFilter = Observable.Never<HierarchyFilter>();
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="FolderNodeViewModel.Children" />: the row of an object that leaves the folder is disposed.
	/// </summary>
	[Test]
	public void Children_Dispose_The_Row_Of_An_Object_That_Leaves_The_Folder()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FolderDto inner = ItemDtoFactory.CreateFolderDto();

		folder.Children.Add(inner);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(folder);

			builder.RegisterInstance(UnusedFilter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FolderNodeViewModel sut = mock.Create<FolderNodeViewModel>();

		ExplorerNodeViewModelBase row = sut.Children.Single();

		// Act
		folder.Children.Remove(inner);

		// Assert
		row.IsDisposed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FolderNodeViewModel.Children" />: the rows follow the objects of the folder as they come and go, in their
	/// order.
	/// </summary>
	[Test]
	public void Children_Follow_The_Objects_Of_The_Folder()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto left = ItemDtoFactory.CreateFileDto();

		FolderDto added = ItemDtoFactory.CreateFolderDto();

		FileDto kept = ItemDtoFactory.CreateFileDto();

		folder.Children.Add(left);

		folder.Children.Add(kept);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(folder);

			builder.RegisterInstance(UnusedFilter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FolderNodeViewModel sut = mock.Create<FolderNodeViewModel>();

		// Act
		folder.Children.Remove(left);

		folder.Children.Insert(0, added);

		// Assert
		sut.Children.Select(static x => x.Model)
			.Should()
			.Equal(added, kept);
	}

	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: the rows inside are disposed with the row of the folder.
	/// </summary>
	[Test]
	public void Dispose_Disposes_The_Rows_Inside()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.Children.Add(ItemDtoFactory.CreateFolderDto());

		folder.Children.Add(ItemDtoFactory.CreateFileDto());

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(folder);

			builder.RegisterInstance(UnusedFilter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FolderNodeViewModel sut = mock.Create<FolderNodeViewModel>();

		ExplorerNodeViewModelBase[] rows = [.. sut.Children];

		// Act
		sut.Dispose();

		// Assert
		rows
			.Should()
			.HaveCount(2)
			.And
			.OnlyContain(static x => x.IsDisposed);
	}

	/// <summary>
	/// <see cref="FolderNodeViewModel.ShownChildren" />: the shown rows follow the objects of the folder as they come, in
	/// their order.
	/// </summary>
	[Test]
	public void ShownChildren_Follow_The_Objects_Of_The_Folder_In_Their_Order()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FolderDto added = ItemDtoFactory.CreateFolderDto();

		FileDto kept = ItemDtoFactory.CreateFileDto();

		folder.Children.Add(kept);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			BehaviorSubject<HierarchyFilter> filter = new(HierarchyFilter.None);

			builder.RegisterInstance(folder);

			builder.RegisterInstance<IObservable<HierarchyFilter>>(filter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FolderNodeViewModel sut = mock.Create<FolderNodeViewModel>();

		// Act
		folder.Children.Insert(0, added);

		// Assert
		sut.ShownChildren.Select(static x => x.Model)
			.Should()
			.Equal(added, kept);
	}

	/// <summary>
	/// <see cref="FolderNodeViewModel.ShownChildren" />: a row the filter hid comes back in its place, alive.
	/// </summary>
	[Test]
	public void ShownChildren_Show_A_Hidden_Row_Again_In_Its_Place()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto first = ItemDtoFactory.CreateFileDto();

		FileDto second = ItemDtoFactory.CreateFileDto();

		folder.Children.Add(first);

		folder.Children.Add(second);

		BehaviorSubject<HierarchyFilter> filter = new(HierarchyFilter.None);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(folder);

			builder.RegisterInstance<IObservable<HierarchyFilter>>(filter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FolderNodeViewModel sut = mock.Create<FolderNodeViewModel>();

		ExplorerNodeViewModelBase[] rows = [.. sut.Children];

		filter.OnNext(new HierarchyFilter
		{
			ExpandedIds = new HashSet<Guid>(),
			Query = "query",
			ShownIds = new HashSet<Guid> { second.Id }
		});

		// Act
		filter.OnNext(HierarchyFilter.None);

		// Assert
		sut.ShownChildren
			.Should()
			.Equal(rows);

		rows[0].IsDisposed
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FolderNodeViewModel.ShownChildren" />: only the rows of the objects the filter shows are shown.
	/// </summary>
	[Test]
	public void ShownChildren_Show_Only_The_Rows_The_Filter_Shows()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto shown = ItemDtoFactory.CreateFileDto();

		folder.Children.Add(ItemDtoFactory.CreateFileDto());

		folder.Children.Add(shown);

		BehaviorSubject<HierarchyFilter> filter = new(HierarchyFilter.None);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(folder);

			builder.RegisterInstance<IObservable<HierarchyFilter>>(filter);

			builder
				.RegisterType<WorkSession>()
				.AsImplementedInterfaces()
				.SingleInstance();
		});

		FolderNodeViewModel sut = mock.Create<FolderNodeViewModel>();

		// Act
		filter.OnNext(new HierarchyFilter
		{
			ExpandedIds = new HashSet<Guid>(),
			Query = "query",
			ShownIds = new HashSet<Guid> { shown.Id }
		});

		// Assert
		sut.ShownChildren.Select(static x => x.Model)
			.Should()
			.Equal(shown);
	}
	#endregion
}
