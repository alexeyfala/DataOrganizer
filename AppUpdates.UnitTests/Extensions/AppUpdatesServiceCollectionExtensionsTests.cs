using AppUpdates.Extensions;
using AppUpdates.Services;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Shared.Common;
using System.Net.Http;
using System.Net.Http.Headers;

namespace AppUpdates.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(AppUpdatesServiceCollectionExtensions)}"" type")]
internal class AppUpdatesServiceCollectionExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="AppUpdatesServiceCollectionExtensions.AddGitHubReleasesClient" />: the client names the app and pins the
	/// format of the answers of GitHub.
	/// </summary>
	[Test]
	public void AddGitHubReleasesClient_Sets_The_Headers_GitHub_Asks_For()
	{
		// Arrange
		ServiceCollection services = [];

		services.AddGitHubReleasesClient();

		using ServiceProvider provider = services.BuildServiceProvider();

		// Act
		using HttpClient client = provider
			.GetRequiredService<IHttpClientFactory>()
			.CreateClient(UpdateCheckService.HttpClientName);

		// Assert
		HttpRequestHeaders headers = client.DefaultRequestHeaders;

		headers.UserAgent.ToString()
			.Should()
			.Be($"{AppInfo.AppName}/{AppInfo.AppVersion}");

		headers.Accept.ToString()
			.Should()
			.Be("application/vnd.github+json");

		headers.GetValues("X-GitHub-Api-Version")
			.Should()
			.Equal("2022-11-28");
	}
	#endregion
}
