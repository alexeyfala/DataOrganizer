using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.ViewModels.Dialogs;
using Diagnostics.Interfaces;
using DialogContracts.Constants;
using DialogContracts.Enums;
using DialogContracts.Interfaces;
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
	/// <see cref="AsyncResultViewModelBase{TResult}.GetResultAsync" />: a dialog shown in its host and closed without an
	/// answer answers with the default only once it has faded out.
	/// </summary>
	[Test]
	public async Task GetResultAsync_Answers_Once_The_Closed_Dialog_Is_Hidden()
	{
		// Arrange
		TaskCompletionSource dialogClosed = new();

		TaskCompletionSource hidden = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogCloser dialogCloser = Substitute.For<IDialogCloser>();

			IShownDialog shown = Substitute.For<IShownDialog>();

			shown
				.WaitUntilHiddenAsync()
				.Returns(hidden.Task);

			dialogCloser
				.FindShownDialog(DialogHostIdentifiers.Main)
				.Returns(shown);

			builder.RegisterInstance(dialogCloser);
		});

		YesNoCancelBoxViewModel sut = mock.Create<YesNoCancelBoxViewModel>();

		Task<YesNoCancelAnswer> answer = sut.GetResultAsync(dialogClosed.Task, YesNoCancelButtons.YesNoCancel);

		// Act
		dialogClosed.SetResult();

		// Time for the answer to come, were it not waiting for the dialog to fade out.
		await Task.Delay(100);

		bool isAnsweredWhileShown = answer.IsCompleted;

		hidden.SetResult();

		YesNoCancelAnswer actual = await answer.WaitAsync(TimeSpan.FromSeconds(5.0));

		// Assert
		isAnsweredWhileShown
			.Should()
			.BeFalse();

		actual
			.Should()
			.Be(YesNoCancelAnswer.Cancel);
	}

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
	/// <see cref="AsyncResultViewModelBase{TResult}.SetResultAsync" />: the answer closes the dialog in the host that shows
	/// it.
	/// </summary>
	[Test]
	public async Task SetResultAsync_Closes_The_Dialog_In_Its_Host()
	{
		// Arrange
		IDialogCloser dialogCloser = Substitute.For<IDialogCloser>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dialogCloser));

		YesNoCancelBoxViewModel sut = mock.Create<YesNoCancelBoxViewModel>();

		sut.DialogHostIdentifier = DialogHostIdentifiers.Notepad;

		// Act
		await sut.SetResultAsync(YesNoCancelAnswer.Yes);

		// Assert
		await dialogCloser
			.Received(1)
			.CloseAsync(DialogHostIdentifiers.Notepad);
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
