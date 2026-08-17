using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Encryption;
using System;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Encryption;

public sealed class KeeperUnlocker : IKeeperUnlocker
{
	#region Data
	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="IEncryptionFailureReporter" />
	private readonly IEncryptionFailureReporter _failureReporter;
	#endregion

	#region Constructors
	public KeeperUnlocker(
		IDialogService dialogService,
		IEncryptionService encryption,
		IEncryptionFailureReporter failureReporter)
	{
		_dialogService = dialogService;

		_encryption = encryption;

		_failureReporter = failureReporter;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<byte[]?> RequestDekAsync(
		Guid keeperId,
		byte[] encryptedDek,
		string header,
		string? label = null,
		CancellationToken token = default,
		[CallerMemberName] string callerName = "")
	{
		using PinnedSecret password = await _dialogService
			.RequestPasswordAsync(header, label, token: token)
			.ConfigureAwait(false);

		if (password.IsEmpty)
		{
			return null;
		}

		using PinnedBuffer passwordBinary = password.ToUtf8Buffer();

		try
		{
			return _encryption.Decrypt(
				encryptedDek,
				passwordBinary,
				ContentIdentity.ForDek(keeperId));
		}
		catch (Exception ex) when (ex is InvalidCredentialException or CryptographicException)
		{
			_failureReporter.Report(ex, callerName);

			return null;
		}
	}
	#endregion
}
