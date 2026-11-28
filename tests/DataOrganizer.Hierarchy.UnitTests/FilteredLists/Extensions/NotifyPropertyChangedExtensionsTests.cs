using AwesomeAssertions;
using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Hierarchy.FilteredLists.Extensions;
using DataOrganizer.Hierarchy.Interfaces;
using Microsoft.Extensions.Time.Testing;
using System;
using System.Collections.Generic;

namespace DataOrganizer.Hierarchy.UnitTests.FilteredLists.Extensions;

/// <summary>
/// Tests of <see cref="NotifyPropertyChangedExtensions" /> type.
/// </summary>
[TestFixture]
internal sealed partial class NotifyPropertyChangedExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="NotifyPropertyChangedExtensions.FilterPredicate{T}" />: a changed search gives a new predicate only
	/// after its pause.
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

		int countBeforeThePause = predicates.Count;

		// Act
		time.Advance(TimeSpan.FromMilliseconds(500));

		// Assert
		countBeforeThePause
			.Should()
			.Be(1);

		// The first predicate is the one of the empty search, given at once.
		predicates[0](new NameHolder("Other"))
			.Should()
			.BeTrue();

		predicates
			.Should()
			.HaveCount(2);

		predicates[1](new NameHolder("hello world"))
			.Should()
			.BeTrue();

		predicates[1](new NameHolder("Other"))
			.Should()
			.BeFalse();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Simple <see cref="INamed" /> implementation for predicate matching tests.
	/// </summary>
	private sealed class NameHolder : INamed
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NameHolder" /> class.
		/// </summary>
		public NameHolder(string name) => Name = name;

		/// <inheritdoc />
		public string Name { get; }
	}

	/// <summary>
	/// Minimal observable source for filter predicate tests.
	/// </summary>
	private sealed partial class Source : ObservableObject
	{
		/// <summary>
		/// Search text the predicates follow.
		/// </summary>
		[ObservableProperty]
		public partial string? Search { get; set; }
	}
	#endregion
}
