using DataOrganizer.DTO.Entities;
using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Encryption;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Encryption;

public sealed class KeeperUnlocker : IKeeperUnlocker
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="IEncryptionFailureReporter" />
	private readonly IEncryptionFailureReporter _failureReporter;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public KeeperUnlocker(
		IDbAccess dbAccess,
		IDialogService dialogService,
		IEncryptionService encryption,
		IEncryptionFailureReporter failureReporter,
		ILogger logger)
	{
		_dbAccess = dbAccess;

		_dialogService = dialogService;

		_encryption = encryption;

		_failureReporter = failureReporter;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<byte[]?> RequestDekAsync(
		FolderModelDto keeper,
		string header,
		string? label = null,
		CancellationToken token = default,
		[CallerMemberName] string callerName = "")
	{
		if (keeper.EncryptedDek is not { } wrapped)
		{
			return null;
		}

		using PinnedSecret password = await _dialogService
			.RequestPasswordAsync(header, label, token: token)
			.ConfigureAwait(false);

		if (password.IsEmpty)
		{
			return null;
		}

		using PinnedBuffer passwordBinary = password.ToUtf8Buffer();

		ContentIdentity identity = ContentIdentity.ForDek(keeper.Id);

		byte[] dek;

		try
		{
			dek = _encryption.Decrypt(
				wrapped,
				passwordBinary,
				identity);
		}
		catch (Exception ex) when (EncryptionFailures.IsCryptographic(ex))
		{
			_failureReporter.Report(ex, callerName);

			return null;
		}

		await RewrapAsync(
			keeper,
			wrapped,
			dek,
			passwordBinary,
			identity,
			token).ConfigureAwait(false);

		return dek;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Writes the wrapper of the DEK at the current derivation cost. The DEK itself does not change,
	/// so a failure leaves a wrapper the same password still opens.
	/// </summary>
	private async Task RewrapAsync(
		FolderModelDto keeper,
		byte[] wrapped,
		byte[] dek,
		PinnedBuffer password,
		ContentIdentity identity,
		CancellationToken token)
	{
		try
		{
			if (_encryption.RewrapIfOutdated(
				wrapped,
				dek,
				password,
				identity) is not { } rewrapped)
			{
				return;
			}

			if (!await _dbAccess.UpdateFolderPropertiesAsync(keeper.Id,
				[
					x => x.SetProperty(x => x.EncryptedDek, rewrapped)
				], token).ConfigureAwait(false))
			{
				return;
			}

			keeper.EncryptedDek = rewrapped;
		}
		catch (OperationCanceledException)
		{
			// The unlock has already succeeded, so an interrupted rewrap is not worth reporting.
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);
		}
	}
	#endregion
}
