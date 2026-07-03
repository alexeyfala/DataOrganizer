using AwesomeAssertions;
using DataOrganizer.Services.Clipboard;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardGate)}"" type")]
internal class ClipboardGateTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClipboardGate.WaitAsync" />: a second waiter stays blocked until the holder releases.
	/// </summary>
	[Test]
	public async Task WaitAsync_Blocks_Second_Waiter_Until_Release()
	{
		// Arrange
		using ClipboardGate sut = new();

		await sut.WaitAsync();

		// Act
		Task second = sut.WaitAsync();

		// Assert
		second
			.IsCompleted
			.Should()
			.BeFalse();

		sut.Release();

		await second;

		second
			.IsCompleted
			.Should()
			.BeTrue();
	}
	#endregion
}
