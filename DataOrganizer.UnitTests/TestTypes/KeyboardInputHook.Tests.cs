using Autofac.Extras.Moq;
using DataOrganizer.Services;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(KeyboardInputHook)}"" type")]
internal class KeyboardInputHookTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="KeyboardInputHook.StopTracking" />.
	/// </summary>
	[Test]
	public void StopTracking_Stops_Hook()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		// Act

		// Assert
	}
	#endregion
}
