using AppUpdates.Dto;
using AppUpdates.Interfaces;
using Shared.Helpers;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace AppUpdates.Services;

/// <inheritdoc cref="IUpdateNotifier" />
public sealed class UpdateNotifier : IUpdateNotifier
{
	#region Data
	/// <inheritdoc cref="IProcessLauncher" />
	private readonly IProcessLauncher _processLauncher;

	/// <inheritdoc cref="IUpdateCheckService" />
	private readonly IUpdateCheckService _updateCheckService;

	/// <inheritdoc cref="IUpdatePrompt" />
	private readonly IUpdatePrompt _updatePrompt;
	#endregion

	#region Constructors
	public UpdateNotifier(
		IProcessLauncher processLauncher,
		IUpdateCheckService updateCheckService,
		IUpdatePrompt updatePrompt)
	{
		_processLauncher = processLauncher;

		_updateCheckService = updateCheckService;

		_updatePrompt = updatePrompt;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task NotifyIfUpdateAvailableAsync(CancellationToken token = default)
	{
		UpdateCheckResult result = await _updateCheckService
			.CheckAsync(token)
			.ConfigureAwait(true);

		if (!result.IsUpdateAvailable
			|| result.ReleaseUrl is not { } url
			|| !Uri.TryCreate(url, UriKind.Absolute, out Uri? releaseUri)
			|| !TransportSecurity.IsSecureOrLoopback(releaseUri))
		{
			return;
		}

		string text = string.Format(
			CultureInfo.CurrentCulture,
			Strings.UpdateAvailablePrompt,
			result.LatestVersion);

		if (!await _updatePrompt
			.ConfirmUpdateAsync(text, token)
			.ConfigureAwait(true))
		{
			return;
		}

		_processLauncher.OpenWithShell(url);
	}
	#endregion
}
