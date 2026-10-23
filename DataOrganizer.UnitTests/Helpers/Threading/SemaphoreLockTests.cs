using AwesomeAssertions;
using DataOrganizer.Helpers.Threading;
using System;
using System.Threading;

namespace DataOrganizer.UnitTests.Helpers.Threading;

[TestFixture(Description = $@"Tests of ""{nameof(SemaphoreLock)}"" type")]
internal class SemaphoreLockTests
{
	#region Methods
	/// <summary>
	/// <see cref="SemaphoreLock.Dispose" />: gives the held semaphore back.
	/// </summary>
	[Test]
	public void Dispose_Gives_The_Semaphore_Back()
	{
		// Arrange
		using SemaphoreSlim semaphore = new(0, 1);

		SemaphoreLock sut = new(semaphore);

		// Act
		sut.Dispose();

		// Assert
		semaphore.CurrentCount
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="SemaphoreLock.Dispose" />: a semaphore its owner disposed meanwhile is left alone.
	/// </summary>
	[Test]
	public void Dispose_Leaves_A_Disposed_Semaphore_Alone()
	{
		// Arrange
		SemaphoreSlim semaphore = new(0, 1);

		semaphore.Dispose();

		SemaphoreLock sut = new(semaphore);

		// Act
		Action dispose = sut.Dispose;

		// Assert
		dispose
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="SemaphoreLock.IsHeld" />: a lock without a semaphore holds nothing and gives nothing back.
	/// </summary>
	[Test]
	public void IsHeld_Is_False_Without_A_Semaphore()
	{
		// Arrange
		SemaphoreLock sut = default;

		// Act
		sut.Dispose();

		// Assert
		sut.IsHeld
			.Should()
			.BeFalse();
	}
	#endregion
}
