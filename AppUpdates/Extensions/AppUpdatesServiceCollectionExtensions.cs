using AppUpdates.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared.Common;
using System;
using System.Net.Http;

namespace AppUpdates.Extensions;

public static class AppUpdatesServiceCollectionExtensions
{
	#region Methods
	/// <summary>
	/// Registers the <see cref="HttpClient" /> that queries the GitHub releases API.
	/// </summary>
	public static IServiceCollection AddGitHubReleasesClient(this IServiceCollection target)
	{
		target.AddHttpClient(UpdateCheckService.HttpClientName, ConfigureGitHubHttpClient);

		return target;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Configures the <see cref="HttpClient" /> used to query the GitHub releases API.
	/// </summary>
	private static void ConfigureGitHubHttpClient(HttpClient client)
	{
		client.Timeout = TimeSpan.FromSeconds(10.0);

		// GitHub requires a User-Agent; Accept and the API version pin the response format.
		client
			.DefaultRequestHeaders
			.UserAgent
			.ParseAdd($"{AppInfo.AppName}/{AppInfo.AppVersion}");

		client
			.DefaultRequestHeaders
			.Accept
			.ParseAdd("application/vnd.github+json");

		client
			.DefaultRequestHeaders
			.Add("X-GitHub-Api-Version", "2022-11-28");
	}
	#endregion
}
