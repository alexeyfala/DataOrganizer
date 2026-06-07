using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using NSubstitute;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(DialogService)}"" type")]
internal class DialogServiceTests
{
	#region Methods
	/// <summary>
	/// <see cref="DialogService.RequestPasswordAsync" />: posts the work to the dispatcher and returns a task that is still pending.
	/// </summary>
	[Test]
	public void RequestPasswordAsync_Posts_Work_To_Dispatcher_And_Returns_Pending_Task()
	{
		// Arrange
		IDispatcherAccessor dispatcher = Substitute.For<IDispatcherAccessor>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dispatcher));

		DialogService sut = mock.Create<DialogService>();

		// Act
		Task<char[]> task = sut.RequestPasswordAsync("header");

		// Assert
		task.IsCompleted
			.Should()
			.BeFalse();

		dispatcher
			.Received(1)
			.Post(Arg.Any<Action>(), Arg.Any<DispatcherPriority>());
	}

	/// <summary>
	/// <see cref="DialogService.RequestPasswordAsync" />: each call returns an independent task instance.
	/// </summary>
	/// <remarks>
	/// Guards against a regression where the underlying <see cref="TaskCompletionSource{TResult}" />
	/// is hoisted from a local variable to a shared field. In that case every caller would await the
	/// same <see cref="Task{TResult}" /> instance, and the second invocation in a session would throw
	/// <see cref="InvalidOperationException" /> on <c>SetResult</c> (a <see cref="TaskCompletionSource{TResult}" />
	/// can transition to a final state only once). Each call must produce an independent task.
	/// </remarks>
	[Test]
	public void RequestPasswordAsync_Returns_Different_Task_Per_Call()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		DialogService sut = mock.Create<DialogService>();

		// Act
		Task<char[]> first = sut.RequestPasswordAsync("h1");

		Task<char[]> second = sut.RequestPasswordAsync("h2");

		// Assert
		first
			.Should()
			.NotBeSameAs(second);
	}
	#endregion
}
