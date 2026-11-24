using AwesomeAssertions;
using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.FilteredLists.Dto;
using DataOrganizer.Hierarchy.FilteredLists.Helpers;
using DataOrganizer.Hierarchy.UnitTests.TestHelpers.Factories;
using DataOrganizer.Hierarchy.UnitTests.TestHelpers.Fakes;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;

namespace DataOrganizer.Hierarchy.UnitTests.FilteredLists.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(FilteredHierarchy)}"" type")]
internal sealed class FilteredHierarchyTests
{
	#region Methods
	/// <summary>
	/// <see cref="FilteredHierarchy.Changes" />: a new subscriber gets the current filter first.
	/// </summary>
	[Test]
	public void Changes_Gives_A_New_Subscriber_The_Current_Filter_First()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		using Subject<string?> queries = new();

		using FilteredHierarchy sut = new([ItemDtoFactory.CreateNamedFileDto("needle")], queries);

		queries.OnNext("needle");

		List<HierarchyFilter> filters = [];

		// Act
		using IDisposable subscription = sut.Changes.Subscribe(filters.Add);

		// Assert
		filters
			.Should()
			.ContainSingle()
			.Which
			.Query
			.Should()
			.Be("needle");
	}

	/// <summary>
	/// <see cref="FilteredHierarchy.Changes" />: a matched folder opens only when an object inside it matches too.
	/// </summary>
	[Test]
	public void Changes_Opens_A_Matched_Folder_Only_When_An_Object_Inside_Matches()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		FolderDto closed = ItemDtoFactory.CreateNamedFolderDto("needle closed");

		FolderDto opened = ItemDtoFactory.CreateNamedFolderDto("needle opened");

		FileDto inside = ItemDtoFactory.CreateNamedFileDto("needle inside");

		closed.Children.Add(ItemDtoFactory.CreateNamedFileDto("other"));

		opened.Children.Add(inside);

		using Subject<string?> queries = new();

		using FilteredHierarchy sut = new([closed, opened], queries);

		List<HierarchyFilter> filters = [];

		using IDisposable subscription = sut.Changes.Subscribe(filters.Add);

		// Act
		queries.OnNext("needle");

		// Assert
		Guid[] shownIds = [closed.Id, opened.Id, inside.Id];

		filters
			.Should()
			.HaveCount(2);

		filters[1].ShownIds
			.Should()
			.BeEquivalentTo(shownIds);

		filters[1].ExpandedIds
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(opened.Id);
	}

	/// <summary>
	/// <see cref="FilteredHierarchy.Changes" />: a blank search shows every object again.
	/// </summary>
	[Test]
	public void Changes_Shows_Every_Object_Again_For_A_Blank_Query()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		using Subject<string?> queries = new();

		using FilteredHierarchy sut = new([ItemDtoFactory.CreateNamedFileDto("needle")], queries);

		List<HierarchyFilter> filters = [];

		using IDisposable subscription = sut.Changes.Subscribe(filters.Add);

		queries.OnNext("needle");

		// Act
		queries.OnNext("  ");

		// Assert
		filters
			.Should()
			.HaveCount(3);

		filters[2]
			.Should()
			.BeSameAs(HierarchyFilter.None);
	}

	/// <summary>
	/// <see cref="FilteredHierarchy.Changes" />: a search shows the matched objects with all their parents and opens the
	/// folders on the way to them.
	/// </summary>
	[Test]
	public void Changes_Shows_The_Matched_Objects_With_Their_Parents()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		FolderDto docs = ItemDtoFactory.CreateNamedFolderDto("docs");

		FolderDto inner = ItemDtoFactory.CreateNamedFolderDto("inner");

		FileDto deepMatch = ItemDtoFactory.CreateNamedFileDto("needle.txt");

		FileDto rootMatch = ItemDtoFactory.CreateNamedFileDto("needle at the root");

		FolderDto other = ItemDtoFactory.CreateNamedFolderDto("other");

		inner.Children.Add(deepMatch);

		inner.Children.Add(ItemDtoFactory.CreateNamedFileDto("other.txt"));

		docs.Children.Add(inner);

		docs.Children.Add(ItemDtoFactory.CreateNamedFileDto("readme"));

		other.Children.Add(ItemDtoFactory.CreateNamedFileDto("x"));

		using Subject<string?> queries = new();

		using FilteredHierarchy sut = new([docs, rootMatch, other], queries);

		List<HierarchyFilter> filters = [];

		using IDisposable subscription = sut.Changes.Subscribe(filters.Add);

		// Act
		queries.OnNext("needle");

		// Assert
		Guid[] shownIds = [docs.Id, inner.Id, deepMatch.Id, rootMatch.Id];

		Guid[] expandedIds = [docs.Id, inner.Id];

		filters
			.Should()
			.HaveCount(2);

		filters[1].Query
			.Should()
			.Be("needle");

		filters[1].ShownIds
			.Should()
			.BeEquivalentTo(shownIds);

		filters[1].ExpandedIds
			.Should()
			.BeEquivalentTo(expandedIds);
	}

	/// <summary>
	/// <see cref="FilteredHierarchy.Changes" />: a blank search while every object is shown gives no new filter.
	/// </summary>
	[Test]
	public void Changes_Skips_A_Blank_Query_While_Every_Object_Is_Shown()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		using Subject<string?> queries = new();

		using FilteredHierarchy sut = new([ItemDtoFactory.CreateNamedFileDto("needle")], queries);

		List<HierarchyFilter> filters = [];

		using IDisposable subscription = sut.Changes.Subscribe(filters.Add);

		// Act
		queries.OnNext(string.Empty);

		// Assert
		filters
			.Should()
			.ContainSingle();
	}

	/// <summary>
	/// <see cref="FilteredHierarchy.Clear" />: the filter shows every object at once, with no search to wait for.
	/// </summary>
	[Test]
	public void Clear_Shows_Every_Object_At_Once()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		using Subject<string?> queries = new();

		using FilteredHierarchy sut = new([ItemDtoFactory.CreateNamedFileDto("needle")], queries);

		List<HierarchyFilter> filters = [];

		using IDisposable subscription = sut.Changes.Subscribe(filters.Add);

		queries.OnNext("needle");

		// Act
		sut.Clear();

		// Assert
		filters
			.Should()
			.HaveCount(3);

		filters[2]
			.Should()
			.BeSameAs(HierarchyFilter.None);
	}

	/// <summary>
	/// <see cref="FilteredHierarchy(IEnumerable{ExplorerItemDtoBase}, IObservable{string})" />: a search reaches the tree
	/// through the context of the thread that made the filter.
	/// </summary>
	[Test]
	public void Constructor_Applies_The_Searches_Through_The_Context_Of_Its_Thread()
	{
		// Arrange
		QueueingSynchronizationContext context = new();

		SynchronizationContext.SetSynchronizationContext(context);

		try
		{
			using Subject<string?> queries = new();

			using FilteredHierarchy sut = new([ItemDtoFactory.CreateNamedFileDto("needle")], queries);

			List<HierarchyFilter> filters = [];

			using IDisposable subscription = sut.Changes.Subscribe(filters.Add);

			// Act
			queries.OnNext("needle");

			// Assert
			int countBeforeTheContextRuns = filters.Count;

			context.RunPosted();

			countBeforeTheContextRuns
				.Should()
				.Be(1);

			filters
				.Should()
				.HaveCount(2);
		}
		finally
		{
			// No context is left behind for the next test on this thread.
			SynchronizationContext.SetSynchronizationContext(null);
		}
	}
	#endregion
}
