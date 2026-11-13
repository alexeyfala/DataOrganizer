using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Extensions.Views;
using Microsoft.Extensions.Time.Testing;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using ObservableExtensions = DataOrganizer.Extensions.Views.ObservableExtensions;

namespace DataOrganizer.UnitTests.Extensions.Views;

[TestFixture(Description = $@"Tests of ""{nameof(ObservableExtensions)}"" type")]
internal class ObservableExtensionsTests
{
	#region Data
	/// <summary>
	/// Delay of the values.
	/// </summary>
	private static readonly TimeSpan Delay = TimeSpan.FromSeconds(0.5);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ObservableExtensions.SetDelay{T}(IObservable{T}, TimeSpan, TimeProvider)" />: of a burst of values only
	/// the last one comes, once the clock has counted the delay from it.
	/// </summary>
	[AvaloniaTest]
	public void SetDelay_Passes_The_Last_Value_After_The_Delay_Of_The_Clock()
	{
		// Arrange
		List<int> received = [];

		FakeTimeProvider time = new();

		Subject<int> subject = new();

		using IDisposable subscription = subject
			.SetDelay(Delay, time)
			.Subscribe(received.Add);

		subject.OnNext(1);

		time.Advance(Delay / 2);

		subject.OnNext(2);

		time.Advance(Delay - TimeSpan.FromTicks(1));

		Dispatcher.UIThread.RunJobs();

		int[] receivedBefore = [.. received];

		// Act
		time.Advance(TimeSpan.FromTicks(1));

		Dispatcher.UIThread.RunJobs();

		// Assert
		receivedBefore
			.Should()
			.BeEmpty();

		received
			.Should()
			.Equal(2);
	}

	/// <summary>
	/// <see cref="ObservableExtensions.SetDelay{T}(IObservable{T}, TimeSpan, TimeProvider)" />: the value comes on the
	/// thread of the context, though the clock counts the delay on another thread.
	/// </summary>
	[AvaloniaTest]
	public void SetDelay_Passes_The_Value_On_The_Thread_Of_The_Context()
	{
		// Arrange
		List<bool> isOnUiThread = [];

		FakeTimeProvider time = new();

		Subject<int> subject = new();

		using IDisposable subscription = subject
			.SetDelay(Delay, time)
			.Subscribe(_ => isOnUiThread.Add(Dispatcher.UIThread.CheckAccess()));

		subject.OnNext(1);

		// Act
		Task
			.Run(() => time.Advance(Delay))
			.Wait();

		Dispatcher.UIThread.RunJobs();

		// Assert
		isOnUiThread
			.Should()
			.Equal(true);
	}

	/// <summary>
	/// <see cref="ObservableExtensions.SetDelay{T}(IObservable{T}, TimeSpan)" />: returns the source unchanged when there is
	/// no sync context.
	/// </summary>
	[Test]
	public void SetDelay_Returns_Source_When_No_Sync_Context()
	{
		// Arrange
		SynchronizationContext.SetSynchronizationContext(null);

		Subject<EventPattern<EventArgs>> subject = new();

		// Act
		IObservable<EventPattern<EventArgs>> result = subject.SetDelay(TimeSpan.FromMilliseconds(100));

		// Assert
		result
			.Should()
			.BeSameAs(subject);
	}
	#endregion
}
