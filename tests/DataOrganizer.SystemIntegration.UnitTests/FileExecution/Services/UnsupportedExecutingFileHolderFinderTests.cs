using AwesomeAssertions;
using DataOrganizer.Shared.Interfaces;
using DataOrganizer.SystemIntegration.FileExecution.Services;
using DataOrganizer.TestSupport.Common;

namespace DataOrganizer.SystemIntegration.UnitTests.FileExecution.Services;

[TestFixture(Description = $@"Tests of ""{nameof(UnsupportedExecutingFileHolderFinder)}"" type")]
internal class UnsupportedExecutingFileHolderFinderTests
{
	#region Methods
	/// <summary>
	/// <see cref="UnsupportedExecutingFileHolderFinder.FindHolders" />: a system that cannot tell the holders of a file
	/// reports none.
	/// </summary>
	[Test]
	public void FindHolders_Finds_None()
	{
		// Arrange
		UnsupportedExecutingFileHolderFinder sut = new();

		// Act
		IExecutingFileProcess[] holders = sut.FindHolders(TestPath.Combine("notes.txt"));

		// Assert
		holders
			.Should()
			.BeEmpty();
	}
	#endregion
}
