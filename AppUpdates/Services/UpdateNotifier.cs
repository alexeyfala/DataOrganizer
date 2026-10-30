using AppUpdates.Dto;
using AppUpdates.Interfaces;
using FileExecution.Interfaces;
using Shared.Helpers;
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
	/// <inheritdoc cref="IProcessManager" />
	private readonly IProcessManager _processManager;

	/// <inheritdoc cref="IUpdateCheckService" />
	private readonly IUpdateCheckService _updateCheckService;

	/// <inheritdoc cref="IUpdatePrompt" />
	private readonly IUpdatePrompt _updatePrompt;
	#endregion

	#region Constructors
	public UpdateNotifier(
		IProcessManager processManager,
		IUpdateCheckService updateCheckService,
		IUpdatePrompt updatePrompt)
	{
		_processManager = processManager;

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

		_processManager.StartProcess(url, out _);
	}
	#endregion
}
