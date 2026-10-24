using AwesomeAssertions;
using System.Threading;
using System.Threading.Tasks;
using Threading.Extensions;
using Threading.Helpers;

namespace Threading.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(SemaphoreSlimExtensions)}"" type")]
internal class SemaphoreSlimExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="SemaphoreSlimExtensions.LockAsync" />: holds the semaphore until the lock is disposed.
	/// </summary>
	[Test]
	public async Task LockAsync_Holds_The_Semaphore_Until_The_Lock_Is_Disposed()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(1, 1);

		// Act
		SemaphoreLock sut = await semaphore.LockAsync();

		int heldCount = semaphore.CurrentCount;

		sut.Dispose();

		// Assert
		(heldCount, semaphore.CurrentCount)
			.Should()
			.Be((0, 1));
	}
	#endregion
}
