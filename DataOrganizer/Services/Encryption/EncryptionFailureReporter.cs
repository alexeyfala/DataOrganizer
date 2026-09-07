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

	/// <inheritdoc cref="ISnackbarService" />
	private readonly ISnackbarService _snackbar;
	#endregion

	#region Constructors
	public EncryptionFailureReporter(ILogger logger, ISnackbarService snackbar)
	{
		_logger = logger;

		_snackbar = snackbar;
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

			_snackbar.ShowError(Strings.IncorrectPassword);

			return;
		}

		_logger.LogException(exception, assertDebug: false);

		string text = exception is CryptographicException
			? Strings.EncryptedDataIsDamaged
			: Strings.FailedToProcessContents;

		_snackbar.ShowError(text);
	}
	#endregion
}
