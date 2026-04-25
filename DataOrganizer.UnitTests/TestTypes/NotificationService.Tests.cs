using Autofac;
using Autofac.Extras.Moq;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Services;
using NSubstitute;
using System;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(NotificationService)}"" type")]
internal class NotificationServiceTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="NotificationService.ShowToast" />.
	/// </summary>
	[Test]
	public void ShowToast_Does_Not_Throw_For_Empty_Or_Null_Message()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		NotificationService sut = mock.Create<NotificationService>();

		// Act
		Action actEmpty = () => sut.ShowToast(string.Empty);

		Action actNull = () => sut.ShowToast(null!);

		// Assert
		actEmpty
			.Should()
			.NotThrow();

		actNull
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// Test of <see cref="NotificationService.ShowToast" />.
	/// </summary>
	[Test]
	public void ShowToast_Posts_Action_To_Dispatcher()
	{
		// Arrange
		IDispatcher dispatcher = Substitute.For<IDispatcher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dispatcher));

		NotificationService sut = mock.Create<NotificationService>();

		// Act
		sut.ShowToast("hello");

		// Assert
		dispatcher
			.Received(1)
			.Post(Arg.Any<Action>());
	}
	#endregion
}
