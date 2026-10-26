using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Services.Documents;
using Microsoft.Extensions.Time.Testing;
using System;
using Threading.Interfaces;
using Threading.UnitTests.Fakes;

namespace DataOrganizer.UnitTests.Services.Documents;

[TestFixture(Description = $@"Tests of ""{nameof(TypingPauseFactory)}"" type")]
internal class TypingPauseFactoryTests
{
	#region Methods
	/// <summary>
	/// <see cref="TypingPauseFactory.Create" />: the pause runs the given action on the clock and the dispatcher of the
	/// application.
	/// </summary>
	[Test]
	public void Create_Makes_A_Pause_That_Runs_The_Action()
	{
		// Arrange
		int runs = 0;

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();

			builder.RegisterInstance<TimeProvider>(time);
		});

		TypingPauseFactory sut = mock.Create<TypingPauseFactory>();

		// Act
		using ITypingPause pause = sut.Create(() => runs++);

		pause.Restart();

		time.Advance(TypingPause.Delay);

		// Assert
		runs
			.Should()
			.Be(1);
	}
	#endregion
}
