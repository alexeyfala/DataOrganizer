using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Services;
using SharpHook;
using SharpHook.Testing;

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
		TestGlobalHook hook = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{

		});

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>(TypedParameter.From<IGlobalHook>(hook));

		_ = hook.RunAsync();

		sut.IsRunning
			.Should()
			.BeTrue();

		// Act
		sut.StopTracking();

		// Assert
		sut.IsRunning
			.Should()
			.BeFalse();
	}
	#endregion
}
