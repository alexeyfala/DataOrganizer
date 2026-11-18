using AwesomeAssertions;
using DataOrganizer.Shared.Threading.Extensions;
using Microsoft.Extensions.Time.Testing;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using ObservableExtensions = DataOrganizer.Shared.Threading.Extensions.ObservableExtensions;

namespace DataOrganizer.Shared.UnitTests.Threading.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(ObservableExtensions)}"" type")]
internal class ObservableExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="ObservableExtensions.WithSearchPause{T}" />: the first value passes at once, a later one only when its
	/// pause ends on the clock.
	/// </summary>
	[Test]
	public void WithSearchPause_Passes_A_Later_Value_When_Its_Pause_Ends()
	{
		// Arrange
		FakeTimeProvider time = new();

		using Subject<string> source = new();

		List<string> values = [];

		using IDisposable subscription = source
			.WithSearchPause(time)
			.Subscribe(values.Add);

		source.OnNext("first");

		source.OnNext("second");

		// Act
		time.Advance(TimeSpan.FromMilliseconds(500) - TimeSpan.FromTicks(1));

		string[] beforeThePauseEnds = [.. values];

		time.Advance(TimeSpan.FromTicks(1));

		// Assert
		beforeThePauseEnds
			.Should()
			.Equal("first");

		values
			.Should()
			.Equal("first", "second");
	}

	/// <summary>
	/// <see cref="ObservableExtensions.WithSearchPause{T}" />: a value typed during the pause starts it again, and only the
	/// last value passes.
	/// </summary>
	[Test]
	public void WithSearchPause_Passes_Only_The_Last_Value_Typed_During_The_Pause()
	{
		// Arrange
		FakeTimeProvider time = new();

		using Subject<string> source = new();

		List<string> values = [];

		using IDisposable subscription = source
			.WithSearchPause(time)
			.Subscribe(values.Add);

		source.OnNext("first");

		source.OnNext("second");

		// Act
		time.Advance(TimeSpan.FromMilliseconds(300));

		source.OnNext("third");

		time.Advance(TimeSpan.FromMilliseconds(300));

		string[] beforeThePauseEnds = [.. values];

		time.Advance(TimeSpan.FromMilliseconds(200));

		// Assert
		beforeThePauseEnds
			.Should()
			.Equal("first");

		values
			.Should()
			.Equal("first", "third");
	}
	#endregion
}
