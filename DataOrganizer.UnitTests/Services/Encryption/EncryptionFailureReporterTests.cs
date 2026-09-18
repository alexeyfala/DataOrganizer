using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Services.Encryption;
using DataOrganizer.UnitTests.Fakes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography;

namespace DataOrganizer.UnitTests.Services.Encryption;

[TestFixture(Description = $@"Tests of ""{nameof(EncryptionFailureReporter)}"" type")]
internal class EncryptionFailureReporterTests
{
	#region Methods
	/// <summary>
	/// <see cref="EncryptionFailureReporter.Report" />: every kind of failure reaches the user as an error,
	/// in words of its own.
	/// </summary>
	[Test]
	public void Report_Tells_About_Every_Failure_In_Words_Of_Its_Own()
	{
		// Arrange
		RecordingNotificationService notification = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<INotificationService>(notification));

		EncryptionFailureReporter sut = mock.Create<EncryptionFailureReporter>();

		Exception[] failures =
		[
			new InvalidCredentialException(),
			new AuthenticationTagMismatchException(),
			new InvalidOperationException()
		];

		List<SnackbarContent?> shown = [];

		// Act
		foreach (Exception failure in failures)
		{
			sut.Report(failure);

			shown.Add(notification.Shown);
		}

		// Assert
		shown
			.Should()
			.OnlyContain(static x => x != null && x.Level == SnackbarMessageLevel.Error);

		shown
			.Select(static x => x!.Text)
			.Should()
			.OnlyHaveUniqueItems();
	}
	#endregion
}
