using AwesomeAssertions;
using DataOrganizer.SystemIntegration.FileExecution.Dto;
using DataOrganizer.SystemIntegration.FileExecution.Services;
using DataOrganizer.TestSupport.Common;
using System.Threading.Tasks;

namespace DataOrganizer.SystemIntegration.UnitTests.FileExecution.Services;

[TestFixture(Description = $@"Tests of ""{nameof(UnsupportedAppPickerService)}"" type")]
internal class UnsupportedAppPickerServiceTests
{
	#region Methods
	/// <summary>
	/// <see cref="UnsupportedAppPickerService.PickAppAsync" />: a system without a picker offers no program.
	/// </summary>
	[Test]
	public async Task PickAppAsync_Picks_No_Program()
	{
		// Arrange
		UnsupportedAppPickerService sut = new();

		// Act
		AssociatedAppInfo? app = await sut.PickAppAsync(TestPath.Combine("notes.txt"));

		// Assert
		app
			.Should()
			.BeNull();
	}
	#endregion
}
