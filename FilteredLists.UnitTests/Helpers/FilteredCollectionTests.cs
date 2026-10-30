using AwesomeAssertions;
using FilteredLists.Helpers;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace FilteredLists.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(FilteredCollection<>)}"" type")]
internal class FilteredCollectionTests
{
	#region Methods
	/// <summary>
	/// <see cref="FilteredCollection{TModel}.AddRange" /> and <see cref="FilteredCollection{TModel}.IsSourceEmpty" />: added items populate both the source and the visible sequence.
	/// </summary>
	[Test]
	public void AddRange_Populates_Source_And_Visible_Sequence()
	{
		// Arrange
		using FilteredCollection<FileDto> sut = CreateSut();

		FileDto[] items = [.. ItemDtoFactory.CreateFileDtos(5)];

		// Act
		sut.AddRange(items);

		// Assert
		sut.IsSourceEmpty
			.Should()
			.BeFalse();

		sut.Visible
			.Should()
			.HaveCount(5);
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.Clear" />: clearing empties both the source and the visible sequence.
	/// </summary>
	[Test]
	public void Clear_Empties_The_Source()
	{
		// Arrange
		using FilteredCollection<FileDto> sut = CreateSut();

		sut.AddRange(ItemDtoFactory.CreateFileDtos(3));

		// Act
		sut.Clear();

		// Assert
		sut.IsSourceEmpty
			.Should()
			.BeTrue();

		sut.Visible
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}(IObservable{Func{TModel, bool}}, System.Linq.Expressions.Expression{Func{TModel, object}})" />:
	/// the changes reach the visible list through the context of the thread that made the collection.
	/// </summary>
	[Test]
	public void Constructor_Hands_The_Changes_To_The_Context_Of_Its_Thread()
	{
		// Arrange
		QueueingSynchronizationContext context = new();

		SynchronizationContext.SetSynchronizationContext(context);

		try
		{
			using FilteredCollection<FileDto> sut = new(Observable.Return<Func<FileDto, bool>>(static _ => true));

			// Act
			sut.AddRange(ItemDtoFactory.CreateFileDtos(3));

			// Assert
			int countBeforeTheContextRuns = sut.Visible.Count;

			context.RunPosted();

			countBeforeTheContextRuns
				.Should()
				.Be(0);

			sut.Visible
				.Should()
				.HaveCount(3);
		}
		finally
		{
			// No context is left behind for the next test on this thread.
			SynchronizationContext.SetSynchronizationContext(null);
		}
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}(IObservable{Func{TModel, bool}}, System.Linq.Expressions.Expression{Func{TModel, object}})" />:
	/// an item whose watched property changes is filtered again.
	/// </summary>
	[Test]
	public void Constructor_Refilters_An_Item_Whose_Watched_Property_Changes()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		FileDto[] items = [.. ItemDtoFactory.CreateFileDtos(2)];

		items[0].Name = "x1";

		items[1].Name = "y";

		using FilteredCollection<FileDto> sut = new(
			Observable.Return<Func<FileDto, bool>>(static x => x.Name.StartsWith('x')),
			static x => x.Name);

		sut.AddRange(items);

		// Act
		items[1].Name = "x2";

		// Assert
		sut.Visible
			.Should()
			.BeEquivalentTo(items);
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.Contains" />: returns true for an existing item and false for an unknown one.
	/// </summary>
	[Test]
	public void Contains_Returns_True_For_Existing_Item()
	{
		// Arrange
		using FilteredCollection<FileDto> sut = CreateSut();

		FileDto[] items = [.. ItemDtoFactory.CreateFileDtos(3)];

		sut.AddRange(items);

		// Act / Assert
		sut.Contains(items[1])
			.Should()
			.BeTrue();

		sut.Contains(ItemDtoFactory.CreateFileDto())
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.Dispose" />: the first disposal empties the visible list.
	/// </summary>
	[Test]
	public void Dispose_Empties_The_Visible_List()
	{
		// Arrange
		FilteredCollection<FileDto> sut = CreateSut();

		sut.AddRange(ItemDtoFactory.CreateFileDtos(3));

		// Act
		sut.Dispose();

		// Assert
		sut.Visible
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.Dispose" />: disposing twice does not throw and clears the source.
	/// </summary>
	[Test]
	public void Dispose_Is_Idempotent_And_Clears_Source()
	{
		// Arrange
		FilteredCollection<FileDto> sut = CreateSut();

		sut.AddRange(ItemDtoFactory.CreateFileDtos(3));

		// Act
		Action act = () =>
		{
			sut.Dispose();

			sut.Dispose();
		};

		// Assert
		act
			.Should()
			.NotThrow();

		sut.Visible
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.FirstOrDefaultFromSource" />: returns the source item matching the predicate.
	/// </summary>
	[Test]
	public void FirstOrDefaultFromSource_Returns_Matching_Item()
	{
		// Arrange
		using FilteredCollection<FileDto> sut = CreateSut();

		FileDto[] items = [.. ItemDtoFactory.CreateFileDtos(3)];

		sut.AddRange(items);

		FileDto target = items[1];

		// Act
		FileDto? result = sut.FirstOrDefaultFromSource(x => x.Id == target.Id);

		// Assert
		result
			.Should()
			.Be(target);
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.FirstOrDefaultFromSource" />: returns null when no source item matches the predicate.
	/// </summary>
	[Test]
	public void FirstOrDefaultFromSource_Returns_Null_If_No_Item_Matches()
	{
		// Arrange
		using FilteredCollection<FileDto> sut = CreateSut();

		sut.AddRange(ItemDtoFactory.CreateFileDtos(3));

		// Act
		FileDto? result = sut.FirstOrDefaultFromSource(x => x.Id == Guid.NewGuid());

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.InsertAndRebuild" />: an already present item is not re-inserted and order is preserved.
	/// </summary>
	[Test]
	public void InsertAndRebuild_Does_Nothing_When_Item_Already_Present()
	{
		// Arrange
		using FilteredCollection<FileDto> sut = CreateSut();

		FileDto[] items = [.. ItemDtoFactory.CreateFileDtos(3)];

		sut.AddRange(items);

		FileDto duplicate = items[2];

		// Act
		sut.InsertAndRebuild(duplicate, 0);

		// Assert
		sut.Visible
			.Should()
			.HaveCount(3);

		sut.Visible
			.Should()
			.ContainInOrder(items);
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.InsertAndRebuild" />: a new item is placed at the specified visible index.
	/// </summary>
	[Test]
	public void InsertAndRebuild_Places_Item_At_Specified_Visible_Index()
	{
		// Arrange
		using FilteredCollection<FileDto> sut = CreateSut();

		sut.AddRange(ItemDtoFactory.CreateFileDtos(3));

		FileDto inserted = ItemDtoFactory.CreateFileDto();

		// Act
		sut.InsertAndRebuild(inserted, 0);

		// Assert
		sut.Visible[0]
			.Should()
			.Be(inserted);
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.InsertAndRebuild" />: an item placed at the first visible index goes to the
	/// start of the source, before the items the filter hides.
	/// </summary>
	[Test]
	public void InsertAndRebuild_Puts_An_Item_At_The_First_Index_Before_The_Hidden_Ones()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		FileDto[] items = [.. ItemDtoFactory.CreateFileDtos(3)];

		FileDto inserted = ItemDtoFactory.CreateFileDto();

		using FilteredCollection<FileDto> sut = new(Observable.Return<Func<FileDto, bool>>(x => x != items[0]));

		sut.AddRange(items);

		// Act
		sut.InsertAndRebuild(inserted, 0);

		// Assert
		sut.SelectFromSource(static x => x)
			.Should()
			.Equal(inserted, items[0], items[1], items[2]);
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.PostToUi" />: the action runs inline when no synchronization context is set.
	/// </summary>
	[Test]
	public void PostToUi_Executes_Action_Inline_When_No_Context()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		using FilteredCollection<FileDto> sut = CreateSut();

		bool executed = false;

		// Act
		sut.PostToUi(() => executed = true);

		// Assert
		executed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.PostToUi" />: with a context the action is posted to it rather than run at once.
	/// </summary>
	[Test]
	public void PostToUi_Posts_The_Action_To_The_Context()
	{
		// Arrange
		QueueingSynchronizationContext context = new();

		SynchronizationContext.SetSynchronizationContext(context);

		try
		{
			using FilteredCollection<FileDto> sut = new(Observable.Return<Func<FileDto, bool>>(static _ => true));

			bool executed = false;

			// Act
			sut.PostToUi(() => executed = true);

			// Assert
			bool isExecutedAtOnce = executed;

			context.RunPosted();

			isExecutedAtOnce
				.Should()
				.BeFalse();

			executed
				.Should()
				.BeTrue();
		}
		finally
		{
			// No context is left behind for the next test on this thread.
			SynchronizationContext.SetSynchronizationContext(null);
		}
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.Remove" />: removing an item returns true and drops it from the source.
	/// </summary>
	[Test]
	public void Remove_Removes_Item_From_Source()
	{
		// Arrange
		using FilteredCollection<FileDto> sut = CreateSut();

		FileDto[] items = [.. ItemDtoFactory.CreateFileDtos(3)];

		sut.AddRange(items);

		// Act
		bool result = sut.Remove(items[1]);

		// Assert
		result
			.Should()
			.BeTrue();

		sut.FirstOrDefaultFromSource(x => x.Id == items[1].Id)
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.Reorder" />: an absent item moves nothing.
	/// </summary>
	[Test]
	public void Reorder_Does_Nothing_For_An_Absent_Item()
	{
		// Arrange
		using FilteredCollection<FileDto> sut = CreateSut();

		FileDto[] items = [.. ItemDtoFactory.CreateFileDtos(3)];

		sut.AddRange(items);

		// Act
		sut.Reorder(ItemDtoFactory.CreateFileDto(), 0);

		// Assert
		sut.Visible
			.Should()
			.Equal(items);
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.Reorder" />: an item already at its place is left there without a rebuild.
	/// </summary>
	[Test]
	public void Reorder_Leaves_An_Item_At_Its_Place_Alone()
	{
		// Arrange
		using FilteredCollection<FileDto> sut = CreateSut();

		FileDto[] items = [.. ItemDtoFactory.CreateFileDtos(3)];

		sut.AddRange(items);

		int changes = 0;

		((INotifyCollectionChanged)sut.Visible).CollectionChanged += (_, _) => changes++;

		// Act
		sut.Reorder(items[1], 1);

		// Assert
		changes
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.Reorder" />: an item moved past the last visible index goes to the end.
	/// </summary>
	[Test]
	public void Reorder_Moves_An_Item_Past_The_End_To_The_End()
	{
		// Arrange
		using FilteredCollection<FileDto> sut = CreateSut();

		FileDto[] items = [.. ItemDtoFactory.CreateFileDtos(3)];

		sut.AddRange(items);

		// Act
		sut.Reorder(items[1], items.Length);

		// Assert
		sut.Visible
			.Should()
			.Equal(items[0], items[2], items[1]);
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.Reorder" />: an item is moved to the specified visible index.
	/// </summary>
	[Test]
	public void Reorder_Moves_Item_To_Specified_Visible_Index()
	{
		// Arrange
		using FilteredCollection<FileDto> sut = CreateSut();

		FileDto[] items = [.. ItemDtoFactory.CreateFileDtos(3)];

		sut.AddRange(items);

		// Act
		sut.Reorder(items[0], 2);

		// Assert
		sut.Visible
			.Should()
			.Equal(items[1], items[2], items[0]);
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.SelectFromSource" />: projects each source item through the selector.
	/// </summary>
	[Test]
	public void SelectFromSource_Projects_Source_Items()
	{
		// Arrange
		using FilteredCollection<FileDto> sut = CreateSut();

		FileDto[] items = [.. ItemDtoFactory.CreateFileDtos(3)];

		sut.AddRange(items);

		// Act
		Guid[] result = [.. sut.SelectFromSource(x => x.Id)];

		// Assert
		result
			.Should()
			.BeEquivalentTo(items.Select(x => x.Id));
	}

	/// <summary>
	/// <see cref="FilteredCollection{TModel}.WhenShown" />: emits the item each time the filter shows it, and never while
	/// it hides it.
	/// </summary>
	[Test]
	public void WhenShown_Emits_The_Item_Once_The_Filter_Shows_It_Again()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		using BehaviorSubject<Func<FileDto, bool>> filter = new(static _ => true);

		using FilteredCollection<FileDto> sut = new(filter);

		FileDto[] items = [.. ItemDtoFactory.CreateFileDtos(3)];

		sut.AddRange(items);

		FileDto watched = items[1];

		List<FileDto> shown = [];

		using IDisposable subscription = sut
			.WhenShown(() => watched)
			.Subscribe(shown.Add);

		filter.OnNext(x => x != watched);

		bool isShownWhileHidden = shown.Count > 0;

		// Act
		filter.OnNext(static _ => true);

		// Assert
		isShownWhileHidden
			.Should()
			.BeFalse();

		shown
			.Should()
			.Equal(watched);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a fresh <see cref="FilteredCollection{T}" /> over <see cref="FileDto" /> with no filter and Index ordering.
	/// </summary>
	private static FilteredCollection<FileDto> CreateSut()
	{
		// Reset synchronization context so DynamicData applies changes inline.
		SynchronizationContext.SetSynchronizationContext(null);

		IObservable<Func<FileDto, bool>> filter = Observable.Return<Func<FileDto, bool>>(_ => true);

		return new FilteredCollection<FileDto>(filter);
	}

	/// <summary>
	/// Synchronization context that keeps the posted callbacks until the test runs them.
	/// </summary>
	private sealed class QueueingSynchronizationContext : SynchronizationContext
	{
		private readonly Queue<(SendOrPostCallback Callback, object? State)> _posted = new();

		public override void Post(SendOrPostCallback d, object? state) => _posted.Enqueue((d, state));

		/// <summary>
		/// Runs the callbacks posted so far, in their order.
		/// </summary>
		public void RunPosted()
		{
			while (_posted.TryDequeue(out (SendOrPostCallback Callback, object? State) posted))
			{
				posted.Callback(posted.State);
			}
		}
	}
	#endregion
}
