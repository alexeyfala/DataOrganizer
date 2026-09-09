using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using DataOrganizer.UnitTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using System;

namespace DataOrganizer.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(NotificationService)}"" type")]
internal class NotificationServiceTests
{
	#region Data
	/// <summary>
	/// Mirrors the number of messages the queue keeps waiting.
	/// </summary>
	private const int MaxWaitingSnackbars = 10;

	/// <summary>
	/// Time after which a shown message may be taken off the screen.
	/// </summary>
	private static readonly TimeSpan WholeTurn = MessageChannelOptions.MessageDuration + TimeSpan.FromSeconds(1.0);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="NotificationService.ShowInformationSnackbar" />: messages on top of the limit are dropped instead of piling up.
	/// </summary>
	[Test]
	public void ShowSnackbar_Drops_A_Message_When_Too_Many_Are_Waiting()
	{
		// Arrange
		ISnackbarPresenter presenter = CreatePresenter();

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, presenter, time));

		NotificationService sut = mock.Create<NotificationService>();

		for (int i = 0; i <= MaxWaitingSnackbars; i++)
		{
			sut.ShowInformationSnackbar($"message {i}");
		}

		// Act
		sut.ShowInformationSnackbar("dropped");

		// Assert
		for (int i = 0; i <= MaxWaitingSnackbars; i++)
		{
			PassTurn(sut, time);
		}

		presenter
			.DidNotReceive()
			.Post(Arg.Is<SnackbarContent>(x => x.Text == "dropped"));
	}

	/// <summary>
	/// <see cref="NotificationService.ShowInformationSnackbar" />: a repeat of the shown message gives it more time instead of waiting in the queue.
	/// </summary>
	[Test]
	public void ShowSnackbar_Gives_A_Repeat_More_Time()
	{
		// Arrange
		ISnackbarPresenter presenter = CreatePresenter();

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, presenter, time));

		NotificationService sut = mock.Create<NotificationService>();

		sut.ShowInformationSnackbar("first");

		time.Advance(WholeTurn);

		// Act
		sut.ShowInformationSnackbar("first");

		bool keepTicking = sut.Tick();

		// Assert
		keepTicking
			.Should()
			.BeTrue();

		presenter
			.Received(1)
			.Post(Arg.Is<SnackbarContent>(x => x.Text == "first"));

		presenter
			.DidNotReceive()
			.Remove();
	}

	/// <summary>
	/// <see cref="NotificationService.ShowInformationSnackbar" />: a message that arrives while another one is shown waits for its turn.
	/// </summary>
	[Test]
	public void ShowSnackbar_Holds_A_Message_While_Another_One_Is_Shown()
	{
		// Arrange
		ISnackbarPresenter presenter = CreatePresenter();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, presenter, new FakeTimeProvider()));

		NotificationService sut = mock.Create<NotificationService>();

		sut.ShowInformationSnackbar("first");

		// Act
		sut.ShowInformationSnackbar("second");

		// Assert
		presenter
			.DidNotReceive()
			.Post(Arg.Is<SnackbarContent>(x => x.Text == "second"));
	}

	/// <summary>
	/// <see cref="NotificationService.ShowInformationSnackbar" />: the first message goes to the host without waiting.
	/// </summary>
	[Test]
	public void ShowSnackbar_Posts_The_First_Message_At_Once()
	{
		// Arrange
		ISnackbarPresenter presenter = CreatePresenter();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, presenter, new FakeTimeProvider()));

		NotificationService sut = mock.Create<NotificationService>();

		// Act
		sut.ShowInformationSnackbar("first");

		// Assert
		presenter
			.Received(1)
			.Post(Arg.Is<SnackbarContent>(x => x.Text == "first"));
	}

	/// <summary>
	/// <see cref="NotificationService.ShowInformationSnackbar" />: nothing is shown while the application has no host.
	/// </summary>
	[Test]
	public void ShowSnackbar_Skips_A_Message_When_There_Is_No_Host()
	{
		// Arrange
		ISnackbarPresenter presenter = Substitute.For<ISnackbarPresenter>();

		presenter
			.CanShow
			.Returns(false);

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, presenter, new FakeTimeProvider()));

		NotificationService sut = mock.Create<NotificationService>();

		// Act
		sut.ShowInformationSnackbar("first");

		// Assert
		presenter
			.DidNotReceive()
			.Post(Arg.Any<SnackbarContent>());
	}

	/// <summary>
	/// <see cref="NotificationService.ShowToast" />: a toast that arrives while another one is shown waits for its turn.
	/// </summary>
	[Test]
	public void ShowToast_Holds_A_Message_While_Another_One_Is_Shown()
	{
		// Arrange
		IToastPresenter toastPresenter = CreateToastPresenter();

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(
			builder => Register(builder, CreatePresenter(), time, toastPresenter));

		NotificationService sut = mock.Create<NotificationService>();

		sut.ShowToast("first");

		// Act
		sut.ShowToast("second");

		// Assert
		toastPresenter
			.Received(1)
			.Post("first");

		toastPresenter
			.DidNotReceive()
			.Post("second");

		// Act
		PassTurn(sut, time);

		// Assert
		toastPresenter
			.Received(1)
			.Post("second");
	}

	/// <summary>
	/// <see cref="NotificationService.Tick" />: waiting messages are dropped once the application has no host left.
	/// </summary>
	[Test]
	public void Tick_Drops_Waiting_Messages_When_The_Host_Is_Gone()
	{
		// Arrange
		ISnackbarPresenter presenter = CreatePresenter();

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, presenter, time));

		NotificationService sut = mock.Create<NotificationService>();

		sut.ShowInformationSnackbar("first");

		sut.ShowInformationSnackbar("second");

		time.Advance(WholeTurn);

		presenter
			.CanShow
			.Returns(false);

		// Act
		bool keepTicking = sut.Tick();

		// Assert
		keepTicking
			.Should()
			.BeFalse();

		presenter
			.DidNotReceive()
			.Post(Arg.Is<SnackbarContent>(x => x.Text == "second"));
	}

	/// <summary>
	/// <see cref="NotificationService.Tick" />: a message under the pointer is left on the screen and holds the next one back.
	/// </summary>
	[Test]
	public void Tick_Holds_The_Message_Under_The_Pointer()
	{
		// Arrange
		ISnackbarPresenter presenter = CreatePresenter();

		presenter
			.IsPointerOverMessage
			.Returns(true);

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, presenter, time));

		NotificationService sut = mock.Create<NotificationService>();

		sut.ShowInformationSnackbar("first");

		sut.ShowInformationSnackbar("second");

		time.Advance(WholeTurn);

		// Act
		bool keepTicking = sut.Tick();

		// Assert
		keepTicking
			.Should()
			.BeTrue();

		presenter
			.DidNotReceive()
			.Remove();

		presenter
			.DidNotReceive()
			.Post(Arg.Is<SnackbarContent>(x => x.Text == "second"));
	}

	/// <summary>
	/// <see cref="NotificationService.Tick" />: waiting messages reach the host in the order they were shown in.
	/// </summary>
	[Test]
	public void Tick_Keeps_The_Order()
	{
		// Arrange
		ISnackbarPresenter presenter = CreatePresenter();

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, presenter, time));

		NotificationService sut = mock.Create<NotificationService>();

		sut.ShowInformationSnackbar("first");

		sut.ShowInformationSnackbar("second");

		sut.ShowInformationSnackbar("third");

		// Act
		PassTurn(sut, time);

		PassTurn(sut, time);

		// Assert
		Received.InOrder(() =>
		{
			presenter.Post(Arg.Is<SnackbarContent>(x => x.Text == "first"));

			presenter.Post(Arg.Is<SnackbarContent>(x => x.Text == "second"));

			presenter.Post(Arg.Is<SnackbarContent>(x => x.Text == "third"));
		});
	}

	/// <summary>
	/// <see cref="NotificationService.Tick" />: a waiting message is left alone until the shown one goes away.
	/// </summary>
	[Test]
	public void Tick_Leaves_A_Message_Waiting_Until_Its_Turn()
	{
		// Arrange
		ISnackbarPresenter presenter = CreatePresenter();

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, presenter, time));

		NotificationService sut = mock.Create<NotificationService>();

		sut.ShowInformationSnackbar("first");

		sut.ShowInformationSnackbar("second");

		// Act
		bool keepTicking = sut.Tick();

		// Assert
		keepTicking
			.Should()
			.BeTrue();

		presenter
			.DidNotReceive()
			.Post(Arg.Is<SnackbarContent>(x => x.Text == "second"));
	}

	/// <summary>
	/// <see cref="NotificationService.Tick" />: the waiting message is shown once the host is free.
	/// </summary>
	[Test]
	public void Tick_Posts_The_Waiting_Message()
	{
		// Arrange
		ISnackbarPresenter presenter = CreatePresenter();

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, presenter, time));

		NotificationService sut = mock.Create<NotificationService>();

		sut.ShowInformationSnackbar("first");

		sut.ShowInformationSnackbar("second");

		// Act
		PassTurn(sut, time);

		// Assert
		presenter
			.Received(1)
			.Post(Arg.Is<SnackbarContent>(x => x.Text == "second"));
	}

	/// <summary>
	/// <see cref="NotificationService.Tick" />: the shown message is taken off the screen when its time is up.
	/// </summary>
	[Test]
	public void Tick_Removes_The_Shown_Message()
	{
		// Arrange
		ISnackbarPresenter presenter = CreatePresenter();

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, presenter, time));

		NotificationService sut = mock.Create<NotificationService>();

		sut.ShowInformationSnackbar("first");

		time.Advance(WholeTurn);

		// Act
		bool keepTicking = sut.Tick();

		// Assert
		keepTicking
			.Should()
			.BeTrue();

		presenter
			.Received(1)
			.Remove();
	}

	/// <summary>
	/// <see cref="NotificationService.Tick" />: the loop stops when the last message has gone away.
	/// </summary>
	[Test]
	public void Tick_Stops_When_Nothing_Is_Waiting()
	{
		// Arrange
		ISnackbarPresenter presenter = CreatePresenter();

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, presenter, time));

		NotificationService sut = mock.Create<NotificationService>();

		sut.ShowInformationSnackbar("first");

		sut.ShowInformationSnackbar("second");

		PassTurn(sut, time);

		time.Advance(WholeTurn);

		sut.Tick();

		time.Advance(WholeTurn);

		// Act
		bool keepTicking = sut.Tick();

		// Assert
		keepTicking
			.Should()
			.BeFalse();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a presenter with a host ready to show messages.
	/// </summary>
	private static ISnackbarPresenter CreatePresenter()
	{
		ISnackbarPresenter presenter = Substitute.For<ISnackbarPresenter>();

		presenter
			.CanShow
			.Returns(true);

		return presenter;
	}

	/// <summary>
	/// Creates a presenter with a window ready to show toasts.
	/// </summary>
	private static IToastPresenter CreateToastPresenter()
	{
		IToastPresenter presenter = Substitute.For<IToastPresenter>();

		presenter
			.CanShow
			.Returns(true);

		return presenter;
	}

	/// <summary>
	/// Lets the shown message go away and the next one take its place.
	/// </summary>
	private static void PassTurn(NotificationService sut, FakeTimeProvider time)
	{
		time.Advance(WholeTurn);

		sut.Tick();

		time.Advance(WholeTurn);

		sut.Tick();
	}

	/// <summary>
	/// Registers the dependencies of the service.
	/// </summary>
	private static void Register(
		ContainerBuilder builder,
		ISnackbarPresenter presenter,
		TimeProvider timeProvider,
		IToastPresenter? toastPresenter = null)
	{
		builder
			.RegisterInstance(new InlineDispatcherAccessor())
			.As<IDispatcherAccessor>();

		builder.RegisterInstance(presenter);

		builder
			.RegisterInstance(timeProvider)
			.As<TimeProvider>();

		if (toastPresenter is null)
		{
			return;
		}

		builder.RegisterInstance(toastPresenter);
	}
	#endregion
}
