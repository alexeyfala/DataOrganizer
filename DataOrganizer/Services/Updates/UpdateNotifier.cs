using DataOrganizer.DTO.Updates;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Updates;
using Shared.Properties;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Updates;

public sealed class UpdateNotifier : IUpdateNotifier
{
	#region Data
	/// <inheritdoc cref="IProcessUtils" />
	private readonly IProcessUtils _processUtils;

	/// <inheritdoc cref="IUpdateCheckService" />
	private readonly IUpdateCheckService _updateCheckService;
	#endregion

	#region Constructors
	public UpdateNotifier(
		IProcessUtils processUtils,
		IUpdateCheckService updateCheckService)
	{
		_processUtils = processUtils;

		_updateCheckService = updateCheckService;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task NotifyIfUpdateAvailableAsync(
		IUpdatePrompt prompt,
		CancellationToken token = default)
	{
		UpdateCheckResult result = await _updateCheckService
			.CheckAsync(token)
			.ConfigureAwait(true);

		if (!result.UpdateAvailable || result.ReleaseUrl is not { } url)
		{
			return;
		}

		string text = string.Format(
			CultureInfo.CurrentCulture,
			Strings.UpdateAvailablePrompt,
			result.LatestVersion);

		if (!await prompt
			.ConfirmUpdateAsync(text, token)
			.ConfigureAwait(true))
		{
			return;
		}

		_processUtils.StartProcess(url, out _);
	}
	#endregion
}
