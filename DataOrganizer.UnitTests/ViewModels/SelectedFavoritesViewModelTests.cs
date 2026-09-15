using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Favorites;
using DataOrganizer.UnitTests.Factories;
using DataOrganizer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(SelectedFavoritesViewModel)}"" type")]
internal class SelectedFavoritesViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: it clears all collections and selection properties.
	/// </summary>
	[Test]
	public void Dispose_Clears_Properties()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		SynchronizationContext.SetSynchronizationContext(null);

		SelectedFavoritesViewModel sut = mock.Create<SelectedFavoritesViewModel>();

		const int count = 5;

		List<FavoriteCategory> categories = [.. FavoriteFactory.CreateFavoriteCategories(count)];

		Guid selectedCategoryId = categories[0].Id;

		// The favorites of the selected category are what fills the favorites collection.
		categories[0]
			.Children
			.AddRange(ItemDtoFactory.CreateFileDtos(count));

		sut.Initialize(
			navigationColumnWidth: RandomValues.CreateDouble(100.0, 300.0),
			selectedCategoryId: selectedCategoryId,
			categories: categories,
			orderedCategoryIds: [.. categories.Select(x => x.Id)],
			selectedPairs: [.. FavoriteFactory.CreateFavoriteSelections(count)]);

		sut.IsFavoritesEmpty
			.Should()
			.BeFalse();

		// Act
		sut.Dispose();

		// Assert
		sut.IsCategoriesEmpty
			.Should()
			.BeTrue();

		sut.IsFavoritesEmpty
			.Should()
			.BeTrue();

		sut.OrderedCategoryIds
			.Should()
			.BeEmpty();

		sut.SelectedPairs
			.Should()
			.BeEmpty();

		sut.SelectedCategory
			.Should()
			.BeNull();

		sut.SelectedFavorite
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SelectedFavoritesViewModel.Initialize" />: it populates the collections and selection properties from the supplied data.
	/// </summary>
	[Test]
	public void Initialize_Initializes_Properties()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		SynchronizationContext.SetSynchronizationContext(null);

		SelectedFavoritesViewModel sut = mock.Create<SelectedFavoritesViewModel>();

		const int count = 5;

		List<FavoriteCategory> categories = [.. FavoriteFactory.CreateFavoriteCategories(count)];

		// Act
		sut.Initialize(
			navigationColumnWidth: RandomValues.CreateDouble(100.0, 300.0),
			selectedCategoryId: categories[0].Id,
			categories: categories,
			orderedCategoryIds: [.. categories.OrderBy(x => x.Name).Select(x => x.Id)],
			selectedPairs: [.. FavoriteFactory.CreateFavoriteSelections(count)]);

		// Assert
		sut.NavigationColumnWidth.Value
			.Should()
			.NotBe(default);

		sut.SelectedCategory
			.Should()
			.NotBeNull();

		sut.IsCategoriesEmpty
			.Should()
			.BeFalse();

		sut.OrderedCategoryIds
			.Should()
			.NotBeEmpty();

		sut.SelectedPairs
			.Should()
			.NotBeEmpty();
	}
	#endregion
}
