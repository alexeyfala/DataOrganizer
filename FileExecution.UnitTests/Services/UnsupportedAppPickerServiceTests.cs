using AwesomeAssertions;
using FileExecution.Dto;
using FileExecution.Services;
using System.Threading.Tasks;
using TestSupport.Common;

namespace FileExecution.UnitTests.Services;

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
