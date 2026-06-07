using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO;
using DataOrganizer.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(SelectedFavoritesViewModel)}"" type")]
internal class SelectedFavoritesViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="SelectedFavoritesViewModel.Dispose" />: it clears all collections and selection properties.
	/// </summary>
	[Test]
	public void Dispose_Clears_Properties()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		SelectedFavoritesViewModel sut = mock.Create<SelectedFavoritesViewModel>();

		const int count = 5;

		sut.AddTestCategories(TestUtils.CreateFavoriteCategories(count));

		sut.AddTestFavorites(TestUtils.CreateFilesDto(count));

		sut
			.OrderedCategories
			.AddRange(TestUtils.CreateGuids(count));

		sut
			.SelectedPairs
			.AddRange(TestUtils.CreateCategoryFavoritePairs(count));

		sut.SelectedCategory = TestUtils.CreateFavoriteCategory();

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
	/// Test of <see cref="SelectedFavoritesViewModel.Initialize" />: it populates the collections and selection properties from the supplied data.
	/// </summary>
	[Test]
	public void Initialize_Initializes_Properties()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		SynchronizationContext.SetSynchronizationContext(null);

		SelectedFavoritesViewModel sut = mock.Create<SelectedFavoritesViewModel>();

		const int count = 5;

		List<FavoriteCategory> categories = [.. TestUtils.CreateFavoriteCategories(count)];

		// Act
		sut.Initialize(
			navigationColumnWidth: TestUtils.CreateRandomDouble(100.0, 300.0),
			selectedCategoryId: categories[0].Id,
			categories: categories,
			orderedCategories: [.. categories.OrderBy(x => x.Name).Select(x => x.Id)],
			selectedPairs: [.. TestUtils.CreateCategoryFavoritePairs(count)]);

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
