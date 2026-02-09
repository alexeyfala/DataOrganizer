using Autofac.Extras.Moq;
using DataOrganizer.ViewModels;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(YesNoCancelBoxViewModel)}"" type")]
internal class YesNoCancelBoxViewModelTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="" />.
	/// </summary>
	[Test]
	public void TestMethod()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		YesNoCancelBoxViewModel sut = mock.Create<YesNoCancelBoxViewModel>();

		// Act

		// Assert
	}
	#endregion
}
