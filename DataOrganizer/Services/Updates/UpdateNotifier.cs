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
	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="IProcessUtils" />
	private readonly IProcessUtils _processUtils;

	/// <inheritdoc cref="IUpdateCheckService" />
	private readonly IUpdateCheckService _updateCheckService;
	#endregion

	#region Constructors
	public UpdateNotifier(
		IDialogService dialogService,
		IProcessUtils processUtils,
		IUpdateCheckService updateCheckService)
	{
		_dialogService = dialogService;

		_processUtils = processUtils;

		_updateCheckService = updateCheckService;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task NotifyIfUpdateAvailableAsync(CancellationToken token = default)
	{
		UpdateCheckResult result = await _updateCheckService
			.CheckAsync(token)
			.ConfigureAwait(true);

		if (!result.UpdateAvailable || result.ReleaseUrl is not { } url)
		{
			return;
		}

		string prompt = string.Format(
			CultureInfo.CurrentCulture,
			Strings.UpdateAvailablePrompt,
			result.LatestVersion);

		if (await _dialogService
			.RequestYesNoDialogAsync(prompt, token)
			.ConfigureAwait(true))
		{
			_processUtils.StartProcess(url, out _);
		}
	}
	#endregion
}
