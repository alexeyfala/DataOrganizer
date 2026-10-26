using DataProtection.Helpers;
using DataProtection.Interfaces;
using Hierarchy.Interfaces;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DataProtection.Services;

/// <inheritdoc cref="IKeeperUnlocker" />
public sealed class KeeperUnlocker : IKeeperUnlocker
{
	#region Data
	/// <inheritdoc cref="IDekRewrapper" />
	private readonly IDekRewrapper _dekRewrapper;

	/// <inheritdoc cref="IPasswordDekWrapper" />
	private readonly IPasswordDekWrapper _encryption;

	/// <inheritdoc cref="IEncryptionFailureReporter" />
	private readonly IEncryptionFailureReporter _failureReporter;

	/// <inheritdoc cref="IKeeperDekWriter" />
	private readonly IKeeperDekWriter _keeperDekWriter;

	/// <inheritdoc cref="IPasswordPrompt" />
	private readonly IPasswordPrompt _passwordPrompt;
	#endregion

	#region Constructors
	public KeeperUnlocker(
		IDekRewrapper dekRewrapper,
		IEncryptionFailureReporter failureReporter,
		IKeeperDekWriter keeperDekWriter,
		IPasswordDekWrapper encryption,
		IPasswordPrompt passwordPrompt)
	{
		_dekRewrapper = dekRewrapper;

		_failureReporter = failureReporter;

		_keeperDekWriter = keeperDekWriter;

		_encryption = encryption;

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

		await _dekRewrapper
			.RewrapAsync(
				wrapped,
				dek,
				passwordBinary,
				ContentIdentity.Dek,
				(rewrapped, writeToken) => _keeperDekWriter.WriteAsync(keeper, rewrapped, writeToken),
				token)
			.ConfigureAwait(false);

		return dek;
	}
	#endregion
}
