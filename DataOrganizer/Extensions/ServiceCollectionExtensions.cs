using DataOrganizer.Services.Updates;
using Microsoft.Extensions.DependencyInjection;
using Shared.Common;
using System;
using System.Net.Http;

namespace DataOrganizer.Extensions;

internal static class ServiceCollectionExtensions
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

	/// <summary>
	/// Registers <typeparamref name="TImplementation" /> as a singleton
	/// <typeparamref name="TService" /> and a <see cref="Lazy{T}" /> wrapper around it.
	/// </summary>
	public static IServiceCollection AddLazySingleton<TService, TImplementation>(this IServiceCollection target)
		where TService : class
		where TImplementation : class, TService
	{
		target.AddSingleton<TService, TImplementation>();

		return AddLazySingleton<TService>(target);
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

	/// <summary>
	/// Registers a singleton <see cref="Lazy{T}" /> wrapper over an already registered
	/// <typeparamref name="TService" />.
	/// </summary>
	private static IServiceCollection AddLazySingleton<TService>(IServiceCollection target)
		where TService : class
	{
		return target.AddSingleton(x => new Lazy<TService>(x.GetRequiredService<TService>));
	}
	#endregion
}
