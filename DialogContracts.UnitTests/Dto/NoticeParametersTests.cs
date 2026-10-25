using AwesomeAssertions;
using DialogContracts.Dto;
using Shared.Common;

namespace DialogContracts.UnitTests.Dto;

[TestFixture(Description = $@"Tests of ""{nameof(NoticeParameters)}"" type")]
internal class NoticeParametersTests
{
	#region Methods
	/// <summary>
	/// <see cref="NoticeParameters.Title" />: a notice that sets no title of its own carries the name of the application.
	/// </summary>
	[Test]
	public void Title_Defaults_To_The_App_Display_Name()
	{
		// Act
		NoticeParameters sut = new()
		{
			Message = "message"
		};

		// Assert
		sut.Title
			.Should()
			.Be(AppInfo.AppDisplayName);
	}
	#endregion
}
