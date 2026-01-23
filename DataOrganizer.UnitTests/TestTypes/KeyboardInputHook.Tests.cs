using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Services;
using SharpHook;
using SharpHook.Testing;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(KeyboardInputHook)}"" type")]
internal class KeyboardInputHookTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="KeyboardInputHook.Dispose" />.
	/// </summary>
	[Test]
	public void Dispose_Disposes_Hook()
	{
		// Arrange
		TestGlobalHook hook = new();

		using AutoMock mock = AutoMock.GetLoose();

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>(TypedParameter.From<IGlobalHook>(hook));

		sut
			.Files
			.AddRange(TestUtils.CreateFilesDto(10));

		sut
			.InputStack
			.AddRange(TestUtils.CreateCodeMaskPairs(10));

		// Act
		sut.Dispose();

		// Assert
		hook.IsDisposed
			.Should()
			.BeTrue();

		sut.Files
			.Should()
			.BeEmpty();

		sut.InputStack
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Test of <see cref="KeyboardInputHook.HandleKeyReleasedAsync" />.
	/// </summary>
	[Test]
	public async Task HandleKeyReleasedAsync_Sets_Text_To_Clipboard()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>();

		// Act

		// Assert
	}

	/// <summary>
	/// Test of <see cref="KeyboardInputHook.StopTracking" />.
	/// </summary>
	[Test]
	public void StopTracking_Stops_Hook()
	{
		// Arrange
		TestGlobalHook hook = new();

		using AutoMock mock = AutoMock.GetLoose();

		KeyboardInputHook sut = mock.Create<KeyboardInputHook>(TypedParameter.From<IGlobalHook>(hook));

		sut
			.Files
			.AddRange(TestUtils.CreateFilesDto(10));

		sut
			.InputStack
			.AddRange(TestUtils.CreateCodeMaskPairs(10));

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

		sut.Files
			.Should()
			.BeEmpty();

		sut.InputStack
			.Should()
			.BeEmpty();
	}
	#endregion
}
