using AwesomeAssertions;
using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Extensions.Collections;
using DataOrganizer.Interfaces.Hierarchy;
using Microsoft.Extensions.Time.Testing;
using System;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.Extensions.Collections;

[TestFixture(Description = $@"Tests of ""{nameof(NotifyPropertyChangedExtensions)}"" type")]
internal partial class NotifyPropertyChangedExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="NotifyPropertyChangedExtensions.FilterPredicate{T}" />: a changed search gives a new predicate only once
	/// its pause ends on the clock.
	/// </summary>
	[Test]
	public void FilterPredicate_Follows_A_Changed_Search_After_Its_Pause()
	{
		// Arrange
		FakeTimeProvider time = new();

		Source source = new();

		List<Func<INamed, bool>> predicates = [];

		using IDisposable subscription = source
			.FilterPredicate(
				x => x.Search,
				time)
			.Subscribe(predicates.Add);

		source.Search = "hello";

		// Act
		time.Advance(TimeSpan.FromMilliseconds(500) - TimeSpan.FromTicks(1));

		int countBeforeThePauseEnds = predicates.Count;

		time.Advance(TimeSpan.FromTicks(1));

		// Assert
		countBeforeThePauseEnds
			.Should()
			.Be(1);

		predicates
			.Should()
			.HaveCount(2);

		predicates[1](new NameHolder("Other"))
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="NotifyPropertyChangedExtensions.FilterPredicate{T}" />: with a null search the initial predicate matches every item.
	/// </summary>
	[Test]
	public void FilterPredicate_Initial_Predicate_Matches_Every_Item_When_Search_Is_Null()
	{
		// Arrange
		Source source = new();

		// Act
		using IDisposable subscription = source
			.FilterPredicate(
				x => x.Search,
				TimeProvider.System)
			.Subscribe(predicate =>
			{
				// Assert
				predicate(new NameHolder("anything"))
					.Should()
					.BeTrue();
			});
	}

	/// <summary>
	/// <see cref="NotifyPropertyChangedExtensions.FilterPredicate{T}" />: the predicate matches items whose name contains the search text and rejects the rest.
	/// </summary>
	[Test]
	public void FilterPredicate_Predicate_Matches_Item_By_Name_When_Search_Is_Provided()
	{
		// Arrange
		Source source = new() { Search = "hello" };

		// Act
		using IDisposable subscription = source
			.FilterPredicate(
				x => x.Search,
				TimeProvider.System)
			.Subscribe(predicate =>
			{
				// Assert
				predicate(new NameHolder("Hello world"))
					.Should()
					.BeTrue();

				predicate(new NameHolder("Other"))
					.Should()
					.BeFalse();
			});
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Simple <see cref="INamed" /> implementation for predicate matching tests.
	/// </summary>
	private sealed class NameHolder : INamed
	{
		public NameHolder(string name) => Name = name;

		public string Name { get; }
	}

	/// <summary>
	/// Minimal observable source for filter predicate tests.
	/// </summary>
	private sealed partial class Source : ObservableObject
	{
		[ObservableProperty]
		public partial string? Search { get; set; }
	}
	#endregion
}
