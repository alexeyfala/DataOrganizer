using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Extensions;
using NSubstitute;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(DspatcherExtensions)}"" type")]
internal class DspatcherExtensionsTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="DspatcherExtensions.PostAsync" />.
	/// </summary>
	[Test]
	public async Task PostAsync_Invokes_Action_On_Dispatcher_And_Completes_Task()
	{
		// Arrange
		IDispatcher dispatcher = Substitute.For<IDispatcher>();

		// Simulate a dispatcher that immediately runs the posted action.
		dispatcher
			.When(x => x.Post(Arg.Any<Action>()))
			.Do(call => call.Arg<Action>().Invoke());

		bool executed = false;

		// Act
		Task task = dispatcher.PostAsync(() => executed = true);

		await task;

		// Assert
		executed
			.Should()
			.BeTrue();

		task.IsCompletedSuccessfully
			.Should()
			.BeTrue();

		dispatcher
			.Received(1)
			.Post(Arg.Any<Action>());
	}

	/// <summary>
	/// Test of <see cref="DspatcherExtensions.PostAsync" />.
	/// </summary>
	[Test]
	public void PostAsync_Returns_Pending_Task_When_Dispatcher_Does_Not_Invoke_Action()
	{
		// Arrange
		IDispatcher dispatcher = Substitute.For<IDispatcher>();

		// Act
		Task task = dispatcher.PostAsync(() => { });

		// Assert
		task.IsCompleted
			.Should()
			.BeFalse();

		dispatcher
			.Received(1)
			.Post(Arg.Any<Action>());
	}
	#endregion
}
