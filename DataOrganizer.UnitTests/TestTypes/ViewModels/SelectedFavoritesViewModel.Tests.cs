using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Dto.Favorites;
using DataOrganizer.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DataOrganizer.UnitTests.TestTypes.ViewModels;

[TestFixture(Description = $@"Tests of ""{nameof(SelectedFavoritesViewModel)}"" type")]
internal class SelectedFavoritesViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="SelectedFavoritesViewModel.Dispose" />: it clears all collections and selection properties.
	/// </summary>
	[Test]
	public void Dispose_Clears_Properties()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		SelectedFavoritesViewModel sut = mock.Create<SelectedFavoritesViewModel>();

		const int count = 5;

		sut.AddTestCategories(TestData.CreateFavoriteCategories(count));

		sut.AddTestFavorites(TestData.CreateFilesDto(count));

		sut
			.OrderedCategories
			.AddRange(TestData.CreateGuids(count));

		sut
			.SelectedPairs
			.AddRange(TestData.CreateCategoryFavoritePairs(count));

		sut.SelectedCategory = TestData.CreateFavoriteCategory();

		// Act
		sut.Dispose();

		// Assert
		sut.IsCategoriesEmpty
			.Should()
			.BeTrue();

		sut.IsFavoritesEmpty
			.Should()
			.BeTrue();

		sut.OrderedCategories
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

		List<FavoriteCategory> categories = [.. TestData.CreateFavoriteCategories(count)];

		// Act
		sut.Initialize(
			navigationColumnWidth: TestData.CreateRandomDouble(100.0, 300.0),
			selectedCategoryId: categories[0].Id,
			categories: categories,
			orderedCategories: [.. categories.OrderBy(x => x.Name).Select(x => x.Id)],
			selectedPairs: [.. TestData.CreateCategoryFavoritePairs(count)]);

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

		sut.OrderedCategories
			.Should()
			.NotBeEmpty();

		sut.SelectedPairs
			.Should()
			.NotBeEmpty();
	}
	#endregion
}
