using AwesomeAssertions;
using DataOrganizer.Helpers.Views;

namespace DataOrganizer.UnitTests.Helpers.Views;

[TestFixture(Description = $@"Tests of ""{nameof(NavigationColumnHelper)}"" type")]
internal class NavigationColumnHelperTests
{
	#region Methods
	/// <summary>
	/// <see cref="NavigationColumnHelper.FitWidth" />: the column keeps its width for a wider view, a view of unknown width
	/// and a view too narrow to leave the column any width.
	/// </summary>
	[TestCase(1000.0)]
	[TestCase(double.NaN)]
	[TestCase(0.0)]
	public void FitWidth_Keeps_The_Column_Width(double viewWidth)
	{
		// Act
		double? result = NavigationColumnHelper.FitWidth(500.0, viewWidth);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="NavigationColumnHelper.FitWidth" />: a narrower view narrows the column below its own width.
	/// </summary>
	[Test]
	public void FitWidth_Narrows_The_Column_Below_A_Narrower_View()
	{
		// Arrange
		const double viewWidth = 250.0;

		// Act
		double? result = NavigationColumnHelper.FitWidth(500.0, viewWidth);

		// Assert
		result
			.Should()
			.BePositive()
			.And
			.BeLessThan(viewWidth);
	}
	#endregion
}
