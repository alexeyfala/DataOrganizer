using AwesomeAssertions;
using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
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
	public void FilterPredicate_Initial_Predicate_Matches_Every_Item_When_Search_Is_Null()
	{
		// Arrange
		Source source = new();

		// Act
		using IDisposable subscription = source
			.FilterPredicate(x => x.Search)
			.Subscribe(predicate =>
			{
				// Assert
				predicate(new NameHolder("anything"))
					.Should()
					.BeTrue();
			});
	}

	/// <summary>
	/// Test of <see cref="NotifyPropertyChangedExtensions.FilterPredicate{T}" />.
	/// </summary>
	[Test]
	public void FilterPredicate_Predicate_Matches_Item_By_Name_When_Search_Is_Provided()
	{
		// Arrange
		Source source = new() { Search = "hello" };

		// Act
		using IDisposable subscription = source
			.FilterPredicate(x => x.Search)
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
		public partial string? Search { get; set; }
	}
	#endregion
}
