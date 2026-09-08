using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using AwesomeAssertions;
using DataOrganizer.DTO;
using DataOrganizer.Interfaces;
using DataOrganizer.Services;
using DataOrganizer.UnitTests.Helpers;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using System;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(NotificationService)}"" type")]
internal class NotificationServiceTests
{
	#region Data
	/// <summary>
	/// Mirrors the number of messages the queue keeps waiting.
	/// </summary>
	private const int MaxWaiting = 10;

	/// <summary>
	/// Time after which a shown message frees the host for the next one.
	/// </summary>
	private static readonly TimeSpan WholeTurn = NotificationService.MessageDuration + TimeSpan.FromSeconds(1.0);
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

		for (int i = 0; i <= MaxWaiting; i++)
		{
			sut.ShowInformationSnackbar($"message {i}");
		}

		// Act
		sut.ShowInformationSnackbar("dropped");

		// Assert
		for (int i = 0; i <= MaxWaiting; i++)
		{
			time.Advance(WholeTurn);

			sut.TickSnackbars();
		}

		presenter
			.DidNotReceive()
			.Post(
				Arg.Is<SnackbarContent>(x => x.Text == "dropped"),
				Arg.Any<TimeSpan>());
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
			.Post(
				Arg.Is<SnackbarContent>(x => x.Text == "second"),
				Arg.Any<TimeSpan>());
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
			.Post(
				Arg.Is<SnackbarContent>(x => x.Text == "first"),
				NotificationService.MessageDuration);
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
			.IsHostLoaded
			.Returns(false);

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, presenter, new FakeTimeProvider()));

		NotificationService sut = mock.Create<NotificationService>();

		// Act
		sut.ShowInformationSnackbar("first");

		// Assert
		presenter
			.DidNotReceive()
			.Post(Arg.Any<SnackbarContent>(), Arg.Any<TimeSpan>());
	}

	/// <summary>
	/// <see cref="NotificationService.TickSnackbars" />: waiting messages are dropped once the application has no host left.
	/// </summary>
	[Test]
	public void TickSnackbars_Drops_Waiting_Messages_When_The_Host_Is_Gone()
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
			.IsHostLoaded
			.Returns(false);

		// Act
		bool keepTicking = sut.TickSnackbars();

		// Assert
		keepTicking
			.Should()
			.BeFalse();

		presenter
			.DidNotReceive()
			.Post(
				Arg.Is<SnackbarContent>(x => x.Text == "second"),
				Arg.Any<TimeSpan>());
	}

	/// <summary>
	/// <see cref="NotificationService.TickSnackbars" />: waiting messages reach the host in the order they were shown in.
	/// </summary>
	[Test]
	public void TickSnackbars_Keeps_The_Order()
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
		time.Advance(WholeTurn);

		sut.TickSnackbars();

		time.Advance(WholeTurn);

		sut.TickSnackbars();

		// Assert
		Received.InOrder(() =>
		{
			presenter.Post(Arg.Is<SnackbarContent>(x => x.Text == "first"), Arg.Any<TimeSpan>());

			presenter.Post(Arg.Is<SnackbarContent>(x => x.Text == "second"), Arg.Any<TimeSpan>());

			presenter.Post(Arg.Is<SnackbarContent>(x => x.Text == "third"), Arg.Any<TimeSpan>());
		});
	}

	/// <summary>
	/// <see cref="NotificationService.TickSnackbars" />: a waiting message is left alone until the shown one goes away.
	/// </summary>
	[Test]
	public void TickSnackbars_Leaves_A_Message_Waiting_Until_Its_Turn()
	{
		// Arrange
		ISnackbarPresenter presenter = CreatePresenter();

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, presenter, time));

		NotificationService sut = mock.Create<NotificationService>();

		sut.ShowInformationSnackbar("first");

		sut.ShowInformationSnackbar("second");

		// Act
		bool keepTicking = sut.TickSnackbars();

		// Assert
		keepTicking
			.Should()
			.BeTrue();

		presenter
			.DidNotReceive()
			.Post(
				Arg.Is<SnackbarContent>(x => x.Text == "second"),
				Arg.Any<TimeSpan>());
	}

	/// <summary>
	/// <see cref="NotificationService.TickSnackbars" />: the waiting message is shown once the host is free.
	/// </summary>
	[Test]
	public void TickSnackbars_Posts_The_Waiting_Message()
	{
		// Arrange
		ISnackbarPresenter presenter = CreatePresenter();

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, presenter, time));

		NotificationService sut = mock.Create<NotificationService>();

		sut.ShowInformationSnackbar("first");

		sut.ShowInformationSnackbar("second");

		time.Advance(WholeTurn);

		// Act
		bool keepTicking = sut.TickSnackbars();

		// Assert
		keepTicking
			.Should()
			.BeTrue();

		presenter
			.Received(1)
			.Post(
				Arg.Is<SnackbarContent>(x => x.Text == "second"),
				NotificationService.MessageDuration);
	}

	/// <summary>
	/// <see cref="NotificationService.TickSnackbars" />: the loop stops when the last message has gone away.
	/// </summary>
	[Test]
	public void TickSnackbars_Stops_When_Nothing_Is_Waiting()
	{
		// Arrange
		ISnackbarPresenter presenter = CreatePresenter();

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => Register(builder, presenter, time));

		NotificationService sut = mock.Create<NotificationService>();

		sut.ShowInformationSnackbar("first");

		sut.ShowInformationSnackbar("second");

		time.Advance(WholeTurn);

		sut.TickSnackbars();

		time.Advance(WholeTurn);

		// Act
		bool keepTicking = sut.TickSnackbars();

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
			.IsHostLoaded
			.Returns(true);

		return presenter;
	}

	/// <summary>
	/// Registers the dependencies of the service.
	/// </summary>
	private static void Register(
		ContainerBuilder builder,
		ISnackbarPresenter presenter,
		TimeProvider timeProvider)
	{
		builder
			.RegisterInstance(Substitute.For<Application>())
			.As<Application>();

		builder
			.RegisterInstance(new InlineDispatcherAccessor())
			.As<IDispatcherAccessor>();

		builder.RegisterInstance(presenter);

		builder
			.RegisterInstance(timeProvider)
			.As<TimeProvider>();
	}
	#endregion

}
