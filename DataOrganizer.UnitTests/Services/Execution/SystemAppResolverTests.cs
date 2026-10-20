using AwesomeAssertions;
using DataOrganizer.Dto.Execution;
using DataOrganizer.Services.Execution;
using System.Threading.Tasks;
using TestSupport.Common;

namespace DataOrganizer.UnitTests.Services.Execution;

[TestFixture(Description = $@"Tests of ""{nameof(SystemAppResolver)}"" type")]
internal class SystemAppResolverTests
{
	#region Methods
	/// <summary>
	/// <see cref="SystemAppResolver.ResolveAsync" />: the operating system chooses the program, and the opening goes on.
	/// </summary>
	[Test]
	public async Task ResolveAsync_Leaves_The_Program_To_The_System()
	{
		// Arrange
		SystemAppResolver sut = new();

		// Act
		AppResolution result = await sut.ResolveAsync(TestPath.Combine("Sandbox", "notes.txt"));

		// Assert
		result
			.Should()
			.Be(new AppResolution(IsCancelled: false, AppPath: null));
	}
	#endregion
}
