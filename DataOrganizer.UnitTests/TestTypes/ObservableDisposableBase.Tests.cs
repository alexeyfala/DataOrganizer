using AwesomeAssertions;
using DataOrganizer.Abstract;
using System;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ObservableDisposableBase)}"" type")]
internal class ObservableDisposableBaseTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="ObservableDisposableBase.Dispose" />.
	/// </summary>
	[Test]
	public void Dispose_Calls_AfterDispose_Once_On_First_Call()
	{
		// Arrange
		Sut sut = new();

		// Act
		sut.Dispose();

		// Assert
		sut.AfterDisposeCallCount
			.Should()
			.Be(1);

		sut.IsDisposed
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="ObservableDisposableBase.Dispose" />.
	/// </summary>
	[Test]
	public void Dispose_Is_Idempotent()
	{
		// Arrange
		Sut sut = new();

		// Act
		Action act = () =>
		{
			sut.Dispose();

			sut.Dispose();

			sut.Dispose();
		};

		// Assert
		act
			.Should()
			.NotThrow();

		sut.AfterDisposeCallCount
			.Should()
			.Be(1);
	}
	#endregion

	#region Service
	/// <summary>
	/// Minimal concrete subclass used to exercise the abstract base.
	/// </summary>
	private sealed class Sut : ObservableDisposableBase
	{
		#region Properties
		/// <summary>
		/// Number of times <see cref="AfterDispose" /> was invoked.
		/// </summary>
		public int AfterDisposeCallCount { get; private set; }
		#endregion

		#region Methods
		/// <inheritdoc />
		protected override void AfterDispose()
		{
			base.AfterDispose();

			AfterDisposeCallCount++;
		}
		#endregion
	}
	#endregion
}
