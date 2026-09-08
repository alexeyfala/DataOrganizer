using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Encryption;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography;

namespace DataOrganizer.Services.Encryption;

public sealed class EncryptionFailureReporter : IEncryptionFailureReporter
{
	#region Data
	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public EncryptionFailureReporter(ILogger logger, INotificationService notification)
	{
		_logger = logger;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Report(Exception exception, [CallerMemberName] string callerName = "")
	{
		if (exception is InvalidCredentialException)
		{
			_logger.LogWarning(
				$"The password, or the derivation cost and the salt beside it, has been rejected: {callerName}");

			_notification.ShowErrorSnackbar(Strings.IncorrectPassword);

			return;
		}

		_logger.LogException(exception, assertDebug: false);

		string text = exception is CryptographicException
			? Strings.EncryptedDataIsDamaged
			: Strings.FailedToProcessContents;

		_notification.ShowErrorSnackbar(text);
	}
	#endregion
}
