using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Services.Diagnostics;
using DataOrganizer.UnitTests.Fakes;
using NSubstitute;
using Repository.Enums;
using Repository.Exceptions;
using Shared.Common;
using System;

namespace DataOrganizer.UnitTests.Services.Diagnostics;

[TestFixture(Description = $@"Tests of ""{nameof(DbFailureReporter)}"" type")]
internal class DbFailureReporterTests
{
	#region Methods
	/// <summary>
	/// <see cref="DbFailureReporter.Report" />: a cancelled operation is passed over without a word to the user.
	/// </summary>
	[Test]
	public void Report_Keeps_A_Cancelled_Operation_Quiet()
	{
		// Arrange
		RecordingNotificationService notification = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<INotificationService>(notification));

		DbFailureReporter sut = mock.Create<DbFailureReporter>();

		// Act
		sut.Report(new OperationCanceledException(), RandomString.Create(10));

		// Assert
		notification
			.Shown
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="DbFailureReporter.Report" />: a refusal that holds for the session is shown to the user once.
	/// </summary>
	[Test]
	public void Report_Shows_A_Refused_Write_Once()
	{
		// Arrange
		INotificationService notification = Substitute.For<INotificationService>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(notification));

		DbFailureReporter sut = mock.Create<DbFailureReporter>();

		DatabaseNotWritableException refusal = new(DbConnectionStatus.FileUnreadable, RandomString.Create(10));

		// Act
		sut.Report(refusal, RandomString.Create(10));

		sut.Report(refusal, RandomString.Create(10));

		sut.Report(refusal, RandomString.Create(10));

		// Assert
		notification
			.Received(1)
			.ShowErrorSnackbar(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DbFailureReporter.Report" />: a failure reaches the user under the text the caller supplied.
	/// </summary>
	[Test]
	public void Report_Tells_About_A_Failure_Under_The_Supplied_Text()
	{
		// Arrange
		string text = RandomString.Create(10);

		RecordingNotificationService notification = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<INotificationService>(notification));

		DbFailureReporter sut = mock.Create<DbFailureReporter>();

		// Act
		sut.Report(new InvalidOperationException(), text);

		// Assert
		notification
			.Shown
			.Should()
			.NotBeNull();

		notification
			.Shown
			.Text
			.Should()
			.Be(text);

		notification
			.Shown
			.Level
			.Should()
			.Be(SnackbarMessageLevel.Error);
	}
	#endregion
}
