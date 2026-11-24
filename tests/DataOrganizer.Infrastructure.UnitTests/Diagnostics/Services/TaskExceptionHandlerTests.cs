using Autofac;
using Autofac.Extras.Moq;
using DataOrganizer.Infrastructure.Diagnostics.Services;
using NSubstitute;
using Serilog;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.Infrastructure.UnitTests.Diagnostics.Services;

[TestFixture(Description = $@"Tests of ""{nameof(TaskExceptionHandler)}"" type")]
internal sealed class TaskExceptionHandlerTests
{
	#region Methods
	/// <summary>
	/// <see cref="TaskExceptionHandler.Watch" />: the exception of a faulted task is logged.
	/// </summary>
	[Test]
	public async Task Watch_Logs_Exception_When_Task_Is_Faulted()
	{
		// Arrange
		TaskCompletionSource logged = new(TaskCreationOptions.RunContinuationsAsynchronously);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			ILogger logger = Substitute.For<ILogger>();

			logger
				.When(x => x.Error(Arg.Any<Exception>(), Arg.Any<string>(), Arg.Any<string>()))
				.Do(_ => logged.TrySetResult());

			builder.RegisterInstance(logger);
		});

		TaskExceptionHandler sut = mock.Create<TaskExceptionHandler>();

		Task faulted = Task.FromException(new InvalidOperationException("boom"));

		// Act
		sut.Watch(faulted);

		// Assert
		// The continuation runs on the thread pool: the entry of the log ends the wait, the timeout only fails a miss.
		await logged
			.Task
			.WaitAsync(TimeSpan.FromSeconds(5));
	}
	#endregion
}
