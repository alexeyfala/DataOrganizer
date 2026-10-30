using AppInstance.Services;
using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using System;

namespace AppInstance.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(AppUptime)}"" type")]
internal class AppUptimeTests
{
	#region Methods
	/// <summary>
	/// <see cref="AppUptime.Elapsed" />: the time is counted from the moment the service is created.
	/// </summary>
	[Test]
	public void Elapsed_Counts_From_The_Creation()
	{
		// Arrange
		FakeTimeProvider time = new();

		time.Advance(TimeSpan.FromMinutes(5.0));

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<TimeProvider>(time));

		AppUptime sut = mock.Create<AppUptime>();

		// Act
		time.Advance(TimeSpan.FromSeconds(42.0));

		// Assert
		sut.Elapsed
			.Should()
			.Be(TimeSpan.FromSeconds(42.0));
	}
	#endregion
}
