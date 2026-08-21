using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
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

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;
	#endregion

	#region Constructors
	public EncryptionFailureReporter(ILogger logger, IMessenger messenger)
	{
		_logger = logger;

		_messenger = messenger;
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

			_messenger.ShowSnackbar(Strings.IncorrectPassword, SnackbarMessageLevel.Error);

			return;
		}

		_logger.LogException(exception, assertDebug: false);

		string text = exception is CryptographicException
			? Strings.EncryptedDataIsDamaged
			: Strings.FailedToProcessContents;

		_messenger.ShowSnackbar(text, SnackbarMessageLevel.Error);
	}
	#endregion
}
