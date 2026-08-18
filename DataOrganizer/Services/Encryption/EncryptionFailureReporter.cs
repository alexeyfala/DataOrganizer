using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Messages;
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
			_logger.LogWarning($"The password has been rejected: {callerName}");

			SendMessage(Strings.IncorrectPassword);

			return;
		}

		_logger.LogException(exception, assertDebug: false);

		SendMessage(exception is CryptographicException
			? Strings.EncryptedDataIsDamaged
			: Strings.FailedToProcessContents);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Sends <see cref="ShowSnackbarMessage" /> to recepient.
	/// </summary>
	private void SendMessage(string message)
	{
		_messenger.Send(new ShowSnackbarMessage(message, SnackbarMessageLevel.Error));
	}
	#endregion
}
