using AwesomeAssertions;
using DataOrganizer.Extensions;
using System;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(Extensions.ObservableExtensions)}"" type")]
internal class ObservableExtensionsTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="ObservableExtensions.SetDelay{TEventArgs}" />: returns the source unchanged when there is no sync context and context is not ignored.
	/// </summary>
	[Test]
	public void SetDelay_Returns_Source_When_No_Sync_Context_And_Not_Ignoring_Context()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		Subject<EventPattern<EventArgs>> subject = new();

		// Act
		IObservable<EventPattern<EventArgs>> result = subject.SetDelay(TimeSpan.FromMilliseconds(100), ignoreContext: false);

		// Assert
		result
			.Should()
			.BeSameAs(subject);
	}

	/// <summary>
	/// Test of <see cref="ObservableExtensions.SetDelay{TEventArgs}" />: returns a new throttled sequence when context is ignored.
	/// </summary>
	[Test]
	public void SetDelay_Returns_Throttled_Sequence_When_Ignoring_Context()
	{
		// Arrange
		Subject<EventPattern<EventArgs>> subject = new();

		// Act
		IObservable<EventPattern<EventArgs>> result = subject.SetDelay(TimeSpan.FromMilliseconds(100), ignoreContext: true);

		// Assert
		result
			.Should()
			.NotBeSameAs(subject);
	}
	#endregion
}
