using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Encryption;
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
	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="IEncryptionFailureReporter" />
	private readonly IEncryptionFailureReporter _failureReporter;

	/// <inheritdoc cref="IKeeperDekWriter" />
	private readonly IKeeperDekWriter _keeperDekWriter;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IPasswordPrompt" />
	private readonly IPasswordPrompt _passwordPrompt;
	#endregion

	#region Constructors
	public KeeperUnlocker(
		IEncryptionService encryption,
		IEncryptionFailureReporter failureReporter,
		IKeeperDekWriter keeperDekWriter,
		ILogger logger,
		IPasswordPrompt passwordPrompt)
	{
		_encryption = encryption;

		_failureReporter = failureReporter;

		_keeperDekWriter = keeperDekWriter;

		_logger = logger;

		_passwordPrompt = passwordPrompt;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<PinnedBuffer?> RequestDekAsync(
		IPasswordKeeper keeper,
		string header,
		string? label = null,
		CancellationToken token = default,
		[CallerMemberName] string callerName = "")
	{
		if (keeper.EncryptedDek is not { } wrapped)
		{
			return null;
		}

		using PinnedSecret password = await _passwordPrompt
			.RequestPasswordAsync(header, label, token: token)
			.ConfigureAwait(false);

		if (password.IsEmpty)
		{
			return null;
		}

		using PinnedBuffer passwordBinary = password.ToUtf8Buffer();

		PinnedBuffer dek;

		try
		{
			dek = _encryption.Decrypt(
				wrapped,
				passwordBinary,
				ContentIdentity.Dek);
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
		IPasswordKeeper keeper,
		byte[] wrapped,
		PinnedBuffer dek,
		PinnedBuffer password,
		CancellationToken token)
	{
		try
		{
			if (_encryption.RewrapIfOutdated(
				wrapped,
				dek,
				password,
				ContentIdentity.Dek) is not { } rewrapped)
			{
				return;
			}

			await _keeperDekWriter
				.WriteAsync(keeper, rewrapped, token)
				.ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			// The unlock has already succeeded, so an interrupted rewrap is not worth reporting.
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);
		}
	}
	#endregion
}
