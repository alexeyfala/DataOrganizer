using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DynamicData.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(FilterEngineExtensions)}"" type")]
internal class FilterEngineExtensionsTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="FilterEngineExtensions.Refresh(FilterEngine{FavoriteCategory})" />.
	/// </summary>
	[Test]
	public void Refresh_Updates_Order_For_FavoriteCategory_According_To_Source_Index()
	{
		// Arrange
		using FilterEngine<FavoriteCategory> filter = CreateCategoryFilter();

		FavoriteCategory[] items = [.. CreateCategories(4)];

		filter.AddRange(items);

		// Act
		filter.Refresh();

		// Assert
		for (int i = 0; i < items.Length; i++)
		{
			items[i].Order
				.Should()
				.Be(i);
		}
	}

	/// <summary>
	/// Test of <see cref="FilterEngineExtensions.Refresh(FilterEngine{FileModelDto})" />.
	/// </summary>
	[Test]
	public void Refresh_Updates_Order_For_FileModelDto_According_To_Source_Index()
	{
		// Arrange
		using FilterEngine<FileModelDto> filter = CreateFileFilter();

		FileModelDto[] items = [.. TestUtils.CreateFilesDto(5)];

		items
			.ToList()
			.ForEach(x => x.Order = -1);

		filter.AddRange(items);

		// Act
		filter.Refresh();

		// Assert
		for (int i = 0; i < items.Length; i++)
		{
			items[i].Order
				.Should()
				.Be(i);
		}
	}
	#endregion

	#region Service
	/// <summary>
	/// Creates a sequence of fresh <see cref="FavoriteCategory" /> values.
	/// </summary>
	private static IEnumerable<FavoriteCategory> CreateCategories(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return new()
			{
				Children = [.. TestUtils.CreateFilesDto(2)],
				EncryptionStatus = EncryptionStatus.Decrypted,
				Id = Guid.NewGuid(),
				Index = i,
				Name = $"category_{i}"
			};
		}
	}

	/// <summary>
	/// Builds a fresh <see cref="FilterEngine{FavoriteCategory}" /> with no filter.
	/// </summary>
	private static FilterEngine<FavoriteCategory> CreateCategoryFilter()
	{
		SynchronizationContext.SetSynchronizationContext(null);

		IObservable<Func<FavoriteCategory, bool>> predicate = Observable.Return<Func<FavoriteCategory, bool>>(_ => true);

		return new(predicate, SortExpressionComparer<FavoriteCategory>.Ascending(x => x.Order));
	}

	/// <summary>
	/// Builds a fresh <see cref="FilterEngine{FileModelDto}" /> with no filter.
	/// </summary>
	private static FilterEngine<FileModelDto> CreateFileFilter()
	{
		SynchronizationContext.SetSynchronizationContext(null);

		IObservable<Func<FileModelDto, bool>> predicate = Observable.Return<Func<FileModelDto, bool>>(_ => true);

		return new(predicate, SortExpressionComparer<FileModelDto>.Ascending(x => x.Index));
	}
	#endregion
}
