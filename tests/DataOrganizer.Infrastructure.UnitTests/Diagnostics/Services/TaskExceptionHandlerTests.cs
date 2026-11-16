using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Infrastructure.Diagnostics.Services;
using NSubstitute;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.Infrastructure.UnitTests.Diagnostics.Services;

[TestFixture(Description = $@"Tests of ""{nameof(TaskExceptionHandler)}"" type")]
internal class TaskExceptionHandlerTests
{
	#region Methods
	/// <summary>
	/// <see cref="TaskExceptionHandler.Watch" />: the exception of a faulted task is logged.
	/// </summary>
	[Test]
	public async Task Watch_Logs_Exception_When_Task_Is_Faulted()
	{
		// Arrange
		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(logger));

		TaskExceptionHandler sut = mock.Create<TaskExceptionHandler>();

		Task faulted = Task.FromException(new InvalidOperationException("boom"));

		// Act
		sut.Watch(faulted);

		// Wait for the OnlyOnFaulted continuation to run.
		for (int i = 0; i < 50 && !logger.ReceivedCalls().Any(); i++)
		{
			await Task.Delay(TimeSpan.FromMilliseconds(20));
		}

		// Assert
		logger
			.ReceivedCalls()
			.Should()
			.NotBeEmpty();
	}
	#endregion
}
