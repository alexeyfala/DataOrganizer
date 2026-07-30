using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Settings;
using DataOrganizer.DTO.Updates;
using DataOrganizer.Interfaces;
using DataOrganizer.Services.Updates;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(UpdateCheckService)}"" type")]
internal class UpdateCheckServiceTests
{
	#region Methods
	/// <summary>
	/// <see cref="UpdateCheckService.CheckAsync" />: carries the version and release URL when an update is available.
	/// </summary>
	[Test]
	public async Task CheckAsync_Carries_Version_And_Url_When_Update_Available()
	{
		// Arrange
		const string url = "https://github.com/alexeyfala/DataOrganizer/releases/tag/v0.2.0";

		Context context = CreateContext(
			currentVersion: "0.1.0",
			responseJson: Releases(("v0.2.0", url, false)));

		// Act
		UpdateCheckResult result = await context
			.Sut
			.CheckAsync();

		// Assert
		result.UpdateAvailable
			.Should()
			.BeTrue();

		result.LatestVersion
			.Should()
			.Be("0.2.0");

		result.ReleaseUrl
			.Should()
			.Be(url);
	}

	/// <summary>
	/// <see cref="UpdateCheckService.CheckAsync" />: records the timestamp after a completed request.
	/// </summary>
	[Test]
	public async Task CheckAsync_Records_Timestamp_On_Completed_Request()
	{
		// Arrange
		Context context = CreateContext(responseJson: "[]");

		// Act
		UpdateCheckResult result = await context
			.Sut
			.CheckAsync();

		// Assert
		result.UpdateAvailable
			.Should()
			.BeFalse();

		context.Settings.LastUpdateCheckUtc
			.Should()
			.Be(context.Time.GetUtcNow());

		context.SettingsStore
			.Received(1)
			.Save();
	}

	/// <summary>
	/// <see cref="UpdateCheckService.CheckAsync" />: reports an update only when the released version is newer.
	/// </summary>
	[TestCase("0.1.0", "v0.2.0", true)]
	[TestCase("0.1.0", "0.2.0", true)]
	[TestCase("0.1.0", "v1.0.0", true)]
	[TestCase("0.1.0", "v0.2.0-beta", true)]
	[TestCase("0.1.0", "v0.1.0", false)]
	[TestCase("0.1.0", "v0.0.9", false)]
	public async Task CheckAsync_Reports_Update_Only_When_Remote_Is_Newer(
		string currentVersion,
		string tag,
		bool expectedUpdate)
	{
		// Arrange
		Context context = CreateContext(
			currentVersion: currentVersion,
			responseJson: Releases((tag, "https://example.test/release", false)));

		// Act
		UpdateCheckResult result = await context
			.Sut
			.CheckAsync();

		// Assert
		result.UpdateAvailable
			.Should()
			.Be(expectedUpdate);
	}

	/// <summary>
	/// <see cref="UpdateCheckService.CheckAsync" />: reports no update on an unsuccessful HTTP status.
	/// </summary>
	[Test]
	public async Task CheckAsync_Returns_None_On_Http_Error()
	{
		// Arrange
		Context context = CreateContext(statusCode: HttpStatusCode.InternalServerError);

		// Act
		UpdateCheckResult result = await context
			.Sut
			.CheckAsync();

		// Assert
		result.UpdateAvailable
			.Should()
			.BeFalse();

		context.SettingsStore
			.DidNotReceive()
			.Save();
	}

	/// <summary>
	/// <see cref="UpdateCheckService.CheckAsync" />: reports no update on a malformed response body.
	/// </summary>
	[Test]
	public async Task CheckAsync_Returns_None_On_Malformed_Json()
	{
		// Arrange
		Context context = CreateContext(responseJson: "{ not json");

		// Act
		UpdateCheckResult result = await context
			.Sut
			.CheckAsync();

		// Assert
		result.UpdateAvailable
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="UpdateCheckService.CheckAsync" />: reports no update and leaves the timestamp untouched on a transport error.
	/// </summary>
	[Test]
	public async Task CheckAsync_Returns_None_On_Transport_Error()
	{
		// Arrange
		Context context = CreateContext(transportError: new HttpRequestException("boom"));

		// Act
		UpdateCheckResult result = await context
			.Sut
			.CheckAsync();

		// Assert
		result.UpdateAvailable
			.Should()
			.BeFalse();

		context.Settings.LastUpdateCheckUtc
			.Should()
			.BeNull();

		context.SettingsStore
			.DidNotReceive()
			.Save();
	}

	/// <summary>
	/// <see cref="UpdateCheckService.CheckAsync" />: reports no update when the running version cannot be parsed.
	/// </summary>
	[Test]
	public async Task CheckAsync_Returns_None_When_Current_Version_Unparseable()
	{
		// Arrange
		Context context = CreateContext(
			currentVersion: "unknown",
			responseJson: Releases(("v0.2.0", "https://example.test/release", false)));

		// Act
		UpdateCheckResult result = await context
			.Sut
			.CheckAsync();

		// Assert
		result.UpdateAvailable
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="UpdateCheckService.CheckAsync" />: performs the check once the throttle window has elapsed.
	/// </summary>
	[Test]
	public async Task CheckAsync_Runs_After_Throttle_Window_Elapsed()
	{
		// Arrange
		Context context = CreateContext(
			sinceLastCheck: TimeSpan.FromDays(2.0),
			currentVersion: "0.1.0",
			responseJson: Releases(("v0.2.0", "https://example.test/release", false)));

		// Act
		UpdateCheckResult result = await context
			.Sut
			.CheckAsync();

		// Assert
		result.UpdateAvailable
			.Should()
			.BeTrue();

		context.Factory
			.Received(1)
			.CreateClient(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="UpdateCheckService.CheckAsync" />: skips the draft release and uses the newest published one.
	/// </summary>
	[Test]
	public async Task CheckAsync_Skips_Draft_Releases()
	{
		// Arrange
		Context context = CreateContext(
			currentVersion: "0.1.0",
			responseJson: Releases(
				("v0.3.0", "https://example.test/draft", true),
				("v0.2.0", "https://example.test/published", false)));

		// Act
		UpdateCheckResult result = await context
			.Sut
			.CheckAsync();

		// Assert
		result.UpdateAvailable
			.Should()
			.BeTrue();

		result.LatestVersion
			.Should()
			.Be("0.2.0");
	}

	/// <summary>
	/// <see cref="UpdateCheckService.CheckAsync" />: does nothing when the opt-out is set.
	/// </summary>
	[Test]
	public async Task CheckAsync_Skips_When_Opted_Out()
	{
		// Arrange
		Context context = CreateContext(
			checkForUpdates: false,
			responseJson: Releases(("v0.2.0", "https://example.test/release", false)));

		// Act
		UpdateCheckResult result = await context
			.Sut
			.CheckAsync();

		// Assert
		result.UpdateAvailable
			.Should()
			.BeFalse();

		context.Factory
			.DidNotReceive()
			.CreateClient(Arg.Any<string>());

		context.SettingsStore
			.DidNotReceive()
			.Save();
	}

	/// <summary>
	/// <see cref="UpdateCheckService.CheckAsync" />: does nothing when the last check is within the throttle window.
	/// </summary>
	[Test]
	public async Task CheckAsync_Skips_Within_Throttle_Window()
	{
		// Arrange
		Context context = CreateContext(
			sinceLastCheck: TimeSpan.FromHours(1.0),
			responseJson: Releases(("v0.2.0", "https://example.test/release", false)));

		// Act
		UpdateCheckResult result = await context
			.Sut
			.CheckAsync();

		// Assert
		result.UpdateAvailable
			.Should()
			.BeFalse();

		context.Factory
			.DidNotReceive()
			.CreateClient(Arg.Any<string>());
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds a service under test wired with a fake clock, settings, and a canned HTTP response.
	/// </summary>
	private static Context CreateContext(
		bool checkForUpdates = true,
		TimeSpan? sinceLastCheck = null,
		string? currentVersion = "0.1.0",
		string responseJson = "[]",
		HttpStatusCode statusCode = HttpStatusCode.OK,
		Exception? transportError = null)
	{
		FakeTimeProvider time = new();

		AppSettings settings = TestUtils.CreateRandomSettings() with
		{
			CheckForUpdates = checkForUpdates,
			LastUpdateCheckUtc = sinceLastCheck is { } elapsed ? time.GetUtcNow() - elapsed : null
		};

		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppVersionProvider versionProvider = Substitute.For<IAppVersionProvider>();

			StubHttpMessageHandler handler = new(transportError is not null
				? () => throw transportError
				: () => new HttpResponseMessage(statusCode)
				{
					Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
				});

			settingsStore
				.Settings
				.Returns(settings);

			versionProvider
				.CurrentVersion
				.Returns(currentVersion);

			factory
				.CreateClient(Arg.Any<string>())
				.Returns(_ => new HttpClient(handler));

			builder.RegisterInstance(factory);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(versionProvider);

			builder.RegisterInstance<IJsonSerializerWrapper>(new JsonSerializerWrapper());

			builder.RegisterInstance<TimeProvider>(time);
		});

		UpdateCheckService sut = mock.Create<UpdateCheckService>();

		return new Context
		{
			Factory = factory,
			Settings = settings,
			SettingsStore = settingsStore,
			Sut = sut,
			Time = time
		};
	}

	/// <summary>
	/// Serializes a GitHub releases array from the given (tag, url, draft) tuples.
	/// </summary>
	private static string Releases(params (string Tag, string Url, bool Draft)[] releases)
	{
		string items = string.Join(
			",",
			releases.Select(static x =>
				$$"""{"tag_name":"{{x.Tag}}","html_url":"{{x.Url}}","draft":{{(x.Draft ? "true" : "false")}},"prerelease":true}"""));

		return $"[{items}]";
	}
	#endregion

	#region Nested Types
	/// <summary>
	/// Bundles the service under test with its captured collaborators.
	/// </summary>
	private sealed class Context
	{
		#region Properties
		public required IHttpClientFactory Factory { get; init; }

		public required AppSettings Settings { get; init; }

		public required IAppSettingsStore SettingsStore { get; init; }

		public required UpdateCheckService Sut { get; init; }

		public required FakeTimeProvider Time { get; init; }
		#endregion
	}

	/// <summary>
	/// Returns a caller-supplied response (or throws) for every request.
	/// </summary>
	private sealed class StubHttpMessageHandler : HttpMessageHandler
	{
		#region Data
		private readonly Func<HttpResponseMessage> _responder;
		#endregion

		#region Constructors
		public StubHttpMessageHandler(Func<HttpResponseMessage> responder) => _responder = responder;
		#endregion

		#region Methods
		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			return Task.FromResult(_responder());
		}
		#endregion
	}
	#endregion
}
