using DataOrganizer.Infrastructure.AppInstance.Interfaces;
using DataOrganizer.Infrastructure.AppUpdates.Dto;
using DataOrganizer.Infrastructure.AppUpdates.Interfaces;
using DataOrganizer.Infrastructure.Settings.Dto;
using DataOrganizer.Infrastructure.Settings.Interfaces;
using DataOrganizer.Shared.Extensions;
using DataOrganizer.Shared.Interfaces;
using Serilog;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Infrastructure.AppUpdates.Services;

/// <inheritdoc cref="IUpdateCheckService" />
public sealed class UpdateCheckService : IUpdateCheckService
{
	#region Data
	/// <summary>
	/// Name of the configured <see cref="HttpClient" /> used for the GitHub API.
	/// </summary>
	public const string HttpClientName = "GitHub";

	/// <summary>
	/// GitHub API endpoint listing the repository releases, newest first.
	/// </summary>
	private const string ReleasesUrl = "https://api.github.com/repos/alexeyfala/DataOrganizer/releases?per_page=10";

	/// <summary>
	/// Minimum interval between two consecutive update checks.
	/// </summary>
	private static readonly TimeSpan CheckInterval = TimeSpan.FromDays(1.0);

	/// <inheritdoc cref="IBuildInfo" />
	private readonly IBuildInfo _buildInfo;

	/// <inheritdoc cref="IHttpClientFactory" />
	private readonly IHttpClientFactory _httpClientFactory;

	/// <inheritdoc cref="IJsonSerializer" />
	private readonly IJsonSerializer _jsonSerializer;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IAppSettingsStore" />
	private readonly IAppSettingsStore _settingsStore;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;

	/// <inheritdoc cref="IAppVersionProvider" />
	private readonly IAppVersionProvider _versionProvider;
	#endregion

	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="UpdateCheckService" /> class.
	/// </summary>
	public UpdateCheckService(
		IBuildInfo buildInfo,
		IHttpClientFactory httpClientFactory,
		IAppSettingsStore settingsStore,
		IJsonSerializer jsonSerializer,
		TimeProvider timeProvider,
		ILogger logger,
		IAppVersionProvider versionProvider)
	{
		_buildInfo = buildInfo;

		_httpClientFactory = httpClientFactory;

		_settingsStore = settingsStore;

		_jsonSerializer = jsonSerializer;

		_timeProvider = timeProvider;

		_logger = logger;

		_versionProvider = versionProvider;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<UpdateCheckResult> CheckAsync(CancellationToken token = default)
	{
		AppSettings settings = _settingsStore.Settings;

		if (_buildInfo.IsDebug || !settings.CheckForUpdates)
		{
			return new UpdateNotAvailable();
		}

		DateTimeOffset now = _timeProvider.GetUtcNow();

		if (settings.LastUpdateCheckAt is { } last && now - last < CheckInterval)
		{
			return new UpdateNotAvailable();
		}

		GitHubRelease? release;

		try
		{
			release = await FetchLatestReleaseAsync(token).ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogException(ex, breakInDebugger: false);

			return new UpdateNotAvailable();
		}

		settings.LastUpdateCheckAt = now;

		UpdateCheckResult result = release?.TagName is { } tag && TryGetNewerVersion(
			tag,
			settings.LastNotifiedVersion,
			out string? latest)
			? new UpdateAvailable
			{
				LatestVersion = latest,
				ReleaseUrl = release.HtmlUrl
			}
			: new UpdateNotAvailable();

		if (result is UpdateAvailable update)
		{
			settings.LastNotifiedVersion = update.LatestVersion;
		}

		_settingsStore.Save();

		return result;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Parses a version string, tolerating a leading "v" and any pre-release or build suffix.
	/// </summary>
	private static bool TryParseVersion(string? value, [NotNullWhen(true)] out Version? version)
	{
		version = null;

		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}

		ReadOnlySpan<char> span = value
			.AsSpan()
			.Trim();

		if (span[0] is 'v' or 'V')
		{
			span = span[1..];
		}

		int suffix = span.IndexOfAny('-', '+');

		if (suffix >= 0)
		{
			span = span[..suffix];
		}

		return Version.TryParse(span, out version);
	}

	/// <summary>
	/// Fetches the newest published (non-draft) release from GitHub.
	/// </summary>
	private async Task<GitHubRelease?> FetchLatestReleaseAsync(CancellationToken token)
	{
		HttpClient client = _httpClientFactory.CreateClient(HttpClientName);

		using HttpResponseMessage response = await client
			.GetAsync(ReleasesUrl, token)
			.ConfigureAwait(false);

		response.EnsureSuccessStatusCode();

		await using Stream stream = await response
			.Content
			.ReadAsStreamAsync(token)
			.ConfigureAwait(false);

		GitHubRelease[]? releases = await _jsonSerializer
			.DeserializeAsync<GitHubRelease[]>(stream, token)
			.ConfigureAwait(false);

		return releases?.FirstOrDefault(static x => !x.Draft);
	}

	/// <summary>
	/// Determines whether the given release tag denotes a version newer than the running one
	/// and newer than the version already offered to the user.
	/// </summary>
	private bool TryGetNewerVersion(
		string tag,
		string? notifiedVersion,
		[NotNullWhen(true)] out string? latest)
	{
		latest = null;

		if (!TryParseVersion(tag, out Version? remote)
			|| !TryParseVersion(_versionProvider.CurrentVersion, out Version? current)
			|| remote <= current)
		{
			return false;
		}

		if (TryParseVersion(notifiedVersion, out Version? notified) && remote <= notified)
		{
			return false;
		}

		latest = remote.ToString();

		return true;
	}
	#endregion
}
