using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Services;
using NSubstitute;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(TaskExceptionHandler)}"" type")]
internal class TaskExceptionHandlerTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="TaskExceptionHandler.Watch" />.
	/// </summary>
	[Test]
	public async Task Watch_Does_Not_Log_When_Task_Completes_Successfully()
	{
		// Arrange
		ILogger logger = Substitute.For<ILogger>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(logger));

		TaskExceptionHandler sut = mock.Create<TaskExceptionHandler>();

		Task succeeded = Task.CompletedTask;

		// Act
		sut.Watch(succeeded);

		await Task.Delay(TimeSpan.FromMilliseconds(50));

		// Assert
		logger
			.ReceivedCalls()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Test of <see cref="TaskExceptionHandler.Watch" />.
	/// </summary>
	[Test]
	public void Watch_Does_Not_Throw_For_Faulted_Task()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		TaskExceptionHandler sut = mock.Create<TaskExceptionHandler>();

		Task faulted = Task.FromException(new InvalidOperationException());

		// Act
		Action act = () => sut.Watch(faulted);

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// Test of <see cref="TaskExceptionHandler.Watch" />.
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
