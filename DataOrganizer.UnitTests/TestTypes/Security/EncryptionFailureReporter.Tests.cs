using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Enums;
using DataOrganizer.Messages;
using DataOrganizer.Services.Encryption;
using Shared.Properties;
using System;
using System.Security.Authentication;
using System.Security.Cryptography;

namespace DataOrganizer.UnitTests.TestTypes.Security;

[TestFixture(Description = $@"Tests of ""{nameof(EncryptionFailureReporter)}"" type")]
internal class EncryptionFailureReporterTests
{
	#region Methods
	/// <summary>
	/// <see cref="EncryptionFailureReporter.Report" />: rejected credentials are reported as a wrong password.
	/// </summary>
	[Test]
	public void Report_Tells_About_A_Wrong_Password()
	{
		// Act
		ShowSnackbarMessage? received = Report(new InvalidCredentialException());

		// Assert
		received
			.Should()
			.NotBeNull();

		received
			.Text
			.Should()
			.Be(Strings.IncorrectPassword);

		received
			.Level
			.Should()
			.Be(SnackbarMessageLevel.Error);
	}

	/// <summary>
	/// <see cref="EncryptionFailureReporter.Report" />: a failure that is neither rejected credentials
	/// nor a cryptographic one is reported as a failure to process the contents.
	/// </summary>
	[Test]
	public void Report_Tells_About_An_Unprocessable_Content()
	{
		// Act
		ShowSnackbarMessage? received = Report(new InvalidOperationException());

		// Assert
		received
			.Should()
			.NotBeNull();

		received
			.Text
			.Should()
			.Be(Strings.FailedToProcessContents);

		received
			.Level
			.Should()
			.Be(SnackbarMessageLevel.Error);
	}

	/// <summary>
	/// <see cref="EncryptionFailureReporter.Report" />: a cryptographic failure is reported as damaged data.
	/// </summary>
	[Test]
	public void Report_Tells_About_Damaged_Data()
	{
		// Act
		ShowSnackbarMessage? received = Report(new AuthenticationTagMismatchException());

		// Assert
		received
			.Should()
			.NotBeNull();

		received
			.Text
			.Should()
			.Be(Strings.EncryptedDataIsDamaged);

		received
			.Level
			.Should()
			.Be(SnackbarMessageLevel.Error);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Reports the failure and returns the snackbar message the reporter has sent.
	/// </summary>
	private static ShowSnackbarMessage? Report(Exception failure)
	{
		StrongReferenceMessenger messenger = new();

		ShowSnackbarMessage? received = null;

		object recipient = new();

		messenger.Register<ShowSnackbarMessage>(recipient, (_, message) => received = message);

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(messenger).As<IMessenger>());

		EncryptionFailureReporter sut = mock.Create<EncryptionFailureReporter>();

		sut.Report(failure);

		return received;
	}
	#endregion
}
