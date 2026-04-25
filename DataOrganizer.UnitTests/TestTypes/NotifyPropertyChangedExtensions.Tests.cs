using AwesomeAssertions;
using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using NSubstitute;
using System;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(NotifyPropertyChangedExtensions)}"" type")]
internal partial class NotifyPropertyChangedExtensionsTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="NotifyPropertyChangedExtensions.FilterPredicate{T}" />.
	/// </summary>
	[Test]
	public void FilterPredicate_Initial_Predicate_Matches_Every_Item_When_StartWith_Is_Null()
	{
		// Arrange
		Source source = new();

		Action emptyStringAction = Substitute.For<Action>();

		// Act
		using IDisposable subscription = source
			.FilterPredicate(x => x.Search, null, emptyStringAction)
			.Subscribe(predicate =>
			{
				// Assert
				predicate(new NameHolder("anything"))
					.Should()
					.BeTrue();
			});

		emptyStringAction
			.Received(0)
			.Invoke();
	}

	/// <summary>
	/// Test of <see cref="NotifyPropertyChangedExtensions.FilterPredicate{T}" />.
	/// </summary>
	[Test]
	public void FilterPredicate_Invokes_EmptyStringAction_When_StartWith_Is_Empty()
	{
		// Arrange
		Source source = new();

		Action emptyStringAction = Substitute.For<Action>();

		// Act
		using IDisposable subscription = source
			.FilterPredicate(x => x.Search, string.Empty, emptyStringAction)
			.Subscribe(_ => { });

		// Assert
		emptyStringAction
			.Received(1)
			.Invoke();
	}

	/// <summary>
	/// Test of <see cref="NotifyPropertyChangedExtensions.FilterPredicate{T}" />.
	/// </summary>
	[Test]
	public void FilterPredicate_Predicate_Matches_Item_By_Name_When_StartWith_Is_Provided()
	{
		// Arrange
		Source source = new();

		Action emptyStringAction = Substitute.For<Action>();

		// Act
		using IDisposable subscription = source
			.FilterPredicate(x => x.Search, "hello", emptyStringAction)
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

	#region Service
	/// <summary>
	/// Simple <see cref="IName" /> implementation for predicate matching tests.
	/// </summary>
	private sealed class NameHolder : IName
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
		private string? _search;
	}
	#endregion
}
