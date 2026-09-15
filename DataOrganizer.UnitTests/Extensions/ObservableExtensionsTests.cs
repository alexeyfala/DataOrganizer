using AwesomeAssertions;
using DataOrganizer.Extensions;
using System;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;
using ObservableExtensions = DataOrganizer.Extensions.ObservableExtensions;

namespace DataOrganizer.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(ObservableExtensions)}"" type")]
internal class ObservableExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="ObservableExtensions.SetDelay{TEventArgs}" />: returns the source unchanged when there is no sync context and context is not ignored.
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
	#endregion
}
