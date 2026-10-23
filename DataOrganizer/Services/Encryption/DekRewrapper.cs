using DataOrganizer.Helpers.Security;
using DataOrganizer.Interfaces.Encryption;
using Serilog;
using Shared.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Encryption;

/// <inheritdoc cref="IDekRewrapper" />
public sealed class DekRewrapper : IDekRewrapper
{
	#region Data
	/// <inheritdoc cref="IPasswordDekWrapper" />
	private readonly IPasswordDekWrapper _encryption;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public DekRewrapper(IPasswordDekWrapper encryption, ILogger logger)
	{
		_encryption = encryption;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task RewrapAsync(
		byte[] wrapped,
		PinnedBuffer dek,
		PinnedBuffer password,
		ContentIdentity identity,
		Func<byte[], CancellationToken, Task> write,
		CancellationToken token = default)
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

			await write(rewrapped, token).ConfigureAwait(false);
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
