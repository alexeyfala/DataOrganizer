using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Enums.Dialogs;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.ViewModels.Dialogs;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.ViewModels.Dialogs;

[TestFixture(Description = $@"Tests of ""{nameof(AsyncResultViewModelBase<>)}"" type")]
internal class AsyncResultViewModelBaseTests
{
	#region Methods
	/// <summary>
	/// <see cref="AsyncResultViewModelBase{TResult}.GetResultAsync" />: a dialog closed without an answer answers with the default.
	/// </summary>
	[Test]
	public async Task GetResultAsync_Answers_With_The_Default_When_The_Dialog_Closes_Without_An_Answer()
	{
		// Arrange
		TaskCompletionSource dialogClosed = new();

		using AutoMock mock = AutoMock.GetLoose();

		YesNoCancelBoxViewModel sut = mock.Create<YesNoCancelBoxViewModel>();

		Task<YesNoCancelAnswer> answer = sut.GetResultAsync(dialogClosed.Task, YesNoCancelButtons.YesNoCancel);

		// Act
		dialogClosed.SetResult();

		YesNoCancelAnswer actual = await answer.WaitAsync(TimeSpan.FromSeconds(5.0));

		// Assert
		actual
			.Should()
			.Be(YesNoCancelAnswer.Cancel);
	}

	/// <summary>
	/// <see cref="AsyncResultViewModelBase{TResult}.GetResultAsync" />: a dialog that failed to show is reported and answers with the default.
	/// </summary>
	[Test]
	public async Task GetResultAsync_Answers_With_The_Default_When_The_Dialog_Fails_To_Show()
	{
		// Arrange
		Task dialogClosed = Task.FromException(new InvalidOperationException());

		ITaskExceptionHandler exceptionHandler = Substitute.For<ITaskExceptionHandler>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(exceptionHandler));

		YesNoCancelBoxViewModel sut = mock.Create<YesNoCancelBoxViewModel>();

		// Act
		YesNoCancelAnswer actual = await sut
			.GetResultAsync(dialogClosed, YesNoCancelButtons.YesNoCancel)
			.WaitAsync(TimeSpan.FromSeconds(5.0));

		// Assert
		actual
			.Should()
			.Be(YesNoCancelAnswer.Cancel);

		exceptionHandler
			.Received(1)
			.Watch(dialogClosed);
	}

	/// <summary>
	/// <see cref="AsyncResultViewModelBase{TResult}.GetResultAsync" />: a cancelled token stops waiting for the answer.
	/// </summary>
	[Test]
	public async Task GetResultAsync_Stops_Waiting_When_The_Token_Is_Cancelled()
	{
		// Arrange
		using CancellationTokenSource cancellation = new();

		cancellation.Cancel();

		using AutoMock mock = AutoMock.GetLoose();

		YesNoCancelBoxViewModel sut = mock.Create<YesNoCancelBoxViewModel>();

		// Act
		Func<Task> act = () => sut.GetResultAsync(
			new TaskCompletionSource().Task,
			YesNoCancelButtons.YesNoCancel,
			cancellation.Token);

		// Assert
		await act
			.Should()
			.ThrowAsync<OperationCanceledException>();
	}

	/// <summary>
	/// <see cref="AsyncResultViewModelBase{TResult}.SetResultAsync" />: the first answer is kept, a later one is ignored.
	/// </summary>
	[Test]
	public async Task SetResultAsync_Keeps_The_First_Answer()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		YesNoCancelBoxViewModel sut = mock.Create<YesNoCancelBoxViewModel>();

		Task<YesNoCancelAnswer> answer = sut.GetResultAsync(new TaskCompletionSource().Task, YesNoCancelButtons.YesNoCancel);

		// Act
		await sut.SetResultAsync(YesNoCancelAnswer.No);

		await sut.SetResultAsync(YesNoCancelAnswer.Yes);

		YesNoCancelAnswer actual = await answer;

		// Assert
		actual
			.Should()
			.Be(YesNoCancelAnswer.No);
	}
	#endregion
}
