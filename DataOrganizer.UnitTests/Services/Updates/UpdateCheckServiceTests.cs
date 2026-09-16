using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Dto.Updates;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Services.Updates;
using DataOrganizer.UnitTests.Factories;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TestSupport.Http;

namespace DataOrganizer.UnitTests.Services.Updates;

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

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();

			StubHttpMessageHandler handler = StubHttpMessageHandler.FromStatusCode(
				HttpStatusCode.OK,
				Releases(("v0.2.0", url, false)));

			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IAppVersionProvider versionProvider = Substitute.For<IAppVersionProvider>();

			factory
				.CreateClient(Arg.Any<string>())
				.Returns(_ => new HttpClient(handler));

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			versionProvider
				.CurrentVersion
				.Returns("0.1.0");

			builder.RegisterInstance(factory);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(versionProvider);

			builder.RegisterType<SystemTextJsonSerializer>().As<IJsonSerializer>();

			builder.RegisterType<FakeTimeProvider>().As<TimeProvider>();
		});

		UpdateCheckService sut = mock.Create<UpdateCheckService>();

		// Act
		UpdateCheckResult result = await sut.CheckAsync();

		// Assert
		result.IsUpdateAvailable
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
	/// <see cref="UpdateCheckService.CheckAsync" />: records the offered version so it is not offered again.
	/// </summary>
	[Test]
	public async Task CheckAsync_Records_Notified_Version_When_Update_Available()
	{
		// Arrange
		AppSettings settings = SettingsFactory.CreateSettings();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();

			StubHttpMessageHandler handler = StubHttpMessageHandler.FromStatusCode(
				HttpStatusCode.OK,
				Releases(("v0.2.0", "https://example.test/release", false)));

			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IAppVersionProvider versionProvider = Substitute.For<IAppVersionProvider>();

			factory
				.CreateClient(Arg.Any<string>())
				.Returns(_ => new HttpClient(handler));

			settingsStore
				.Settings
				.Returns(settings);

			versionProvider
				.CurrentVersion
				.Returns("0.1.0");

			builder.RegisterInstance(factory);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(versionProvider);

			builder.RegisterType<SystemTextJsonSerializer>().As<IJsonSerializer>();

			builder.RegisterType<FakeTimeProvider>().As<TimeProvider>();
		});

		UpdateCheckService sut = mock.Create<UpdateCheckService>();

		// Act
		await sut.CheckAsync();

		// Assert
		settings.LastNotifiedVersion
			.Should()
			.Be("0.2.0");
	}

	/// <summary>
	/// <see cref="UpdateCheckService.CheckAsync" />: records the timestamp after a completed request.
	/// </summary>
	[Test]
	public async Task CheckAsync_Records_Timestamp_On_Completed_Request()
	{
		// Arrange
		AppSettings settings = SettingsFactory.CreateSettings();

		FakeTimeProvider time = new();

		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();

			StubHttpMessageHandler handler = StubHttpMessageHandler.FromStatusCode(HttpStatusCode.OK, "[]");

			factory
				.CreateClient(Arg.Any<string>())
				.Returns(_ => new HttpClient(handler));

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(factory);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance<TimeProvider>(time);

			builder.RegisterType<SystemTextJsonSerializer>().As<IJsonSerializer>();
		});

		UpdateCheckService sut = mock.Create<UpdateCheckService>();

		// Act
		UpdateCheckResult result = await sut.CheckAsync();

		// Assert
		result.IsUpdateAvailable
			.Should()
			.BeFalse();

		settings.LastUpdateCheckAt
			.Should()
			.Be(time.GetUtcNow());

		settingsStore
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
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();

			StubHttpMessageHandler handler = StubHttpMessageHandler.FromStatusCode(
				HttpStatusCode.OK,
				Releases((tag, "https://example.test/release", false)));

			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IAppVersionProvider versionProvider = Substitute.For<IAppVersionProvider>();

			factory
				.CreateClient(Arg.Any<string>())
				.Returns(_ => new HttpClient(handler));

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			versionProvider
				.CurrentVersion
				.Returns(currentVersion);

			builder.RegisterInstance(factory);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(versionProvider);

			builder.RegisterType<SystemTextJsonSerializer>().As<IJsonSerializer>();

			builder.RegisterType<FakeTimeProvider>().As<TimeProvider>();
		});

		UpdateCheckService sut = mock.Create<UpdateCheckService>();

		// Act
		UpdateCheckResult result = await sut.CheckAsync();

		// Assert
		result.IsUpdateAvailable
			.Should()
			.Be(expectedUpdate);
	}

	/// <summary>
	/// <see cref="UpdateCheckService.CheckAsync" />: offers a version newer than the one already offered.
	/// </summary>
	[Test]
	public async Task CheckAsync_Reports_Update_When_Newer_Than_Notified()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();

			StubHttpMessageHandler handler = StubHttpMessageHandler.FromStatusCode(
				HttpStatusCode.OK,
				Releases(("v0.3.0", "https://example.test/release", false)));

			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IAppVersionProvider versionProvider = Substitute.For<IAppVersionProvider>();

			factory
				.CreateClient(Arg.Any<string>())
				.Returns(_ => new HttpClient(handler));

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					LastNotifiedVersion = "0.2.0"
				});

			versionProvider
				.CurrentVersion
				.Returns("0.1.0");

			builder.RegisterInstance(factory);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(versionProvider);

			builder.RegisterType<SystemTextJsonSerializer>().As<IJsonSerializer>();

			builder.RegisterType<FakeTimeProvider>().As<TimeProvider>();
		});

		UpdateCheckService sut = mock.Create<UpdateCheckService>();

		// Act
		UpdateCheckResult result = await sut.CheckAsync();

		// Assert
		result.IsUpdateAvailable
			.Should()
			.BeTrue();

		result.LatestVersion
			.Should()
			.Be("0.3.0");
	}

	/// <summary>
	/// <see cref="UpdateCheckService.CheckAsync" />: reports no update on an unsuccessful HTTP status.
	/// </summary>
	[Test]
	public async Task CheckAsync_Returns_None_On_Http_Error()
	{
		// Arrange
		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();

			StubHttpMessageHandler handler = StubHttpMessageHandler.FromStatusCode(HttpStatusCode.InternalServerError);

			factory
				.CreateClient(Arg.Any<string>())
				.Returns(_ => new HttpClient(handler));

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			builder.RegisterInstance(factory);

			builder.RegisterInstance(settingsStore);

			builder.RegisterType<FakeTimeProvider>().As<TimeProvider>();
		});

		UpdateCheckService sut = mock.Create<UpdateCheckService>();

		// Act
		UpdateCheckResult result = await sut.CheckAsync();

		// Assert
		result.IsUpdateAvailable
			.Should()
			.BeFalse();

		settingsStore
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
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();

			StubHttpMessageHandler handler = StubHttpMessageHandler.FromStatusCode(HttpStatusCode.OK, "{ not json");

			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			factory
				.CreateClient(Arg.Any<string>())
				.Returns(_ => new HttpClient(handler));

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			builder.RegisterInstance(factory);

			builder.RegisterInstance(settingsStore);

			builder.RegisterType<SystemTextJsonSerializer>().As<IJsonSerializer>();

			builder.RegisterType<FakeTimeProvider>().As<TimeProvider>();
		});

		UpdateCheckService sut = mock.Create<UpdateCheckService>();

		// Act
		UpdateCheckResult result = await sut.CheckAsync();

		// Assert
		result.IsUpdateAvailable
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
		AppSettings settings = SettingsFactory.CreateSettings();

		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();

			StubHttpMessageHandler handler = new(static _ => throw new HttpRequestException("boom"));

			factory
				.CreateClient(Arg.Any<string>())
				.Returns(_ => new HttpClient(handler));

			settingsStore
				.Settings
				.Returns(settings);

			builder.RegisterInstance(factory);

			builder.RegisterInstance(settingsStore);

			builder.RegisterType<FakeTimeProvider>().As<TimeProvider>();
		});

		UpdateCheckService sut = mock.Create<UpdateCheckService>();

		// Act
		UpdateCheckResult result = await sut.CheckAsync();

		// Assert
		result.IsUpdateAvailable
			.Should()
			.BeFalse();

		settings.LastUpdateCheckAt
			.Should()
			.BeNull();

		settingsStore
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
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();

			StubHttpMessageHandler handler = StubHttpMessageHandler.FromStatusCode(
				HttpStatusCode.OK,
				Releases(("v0.2.0", "https://example.test/release", false)));

			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IAppVersionProvider versionProvider = Substitute.For<IAppVersionProvider>();

			factory
				.CreateClient(Arg.Any<string>())
				.Returns(_ => new HttpClient(handler));

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			versionProvider
				.CurrentVersion
				.Returns("unknown");

			builder.RegisterInstance(factory);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(versionProvider);

			builder.RegisterType<SystemTextJsonSerializer>().As<IJsonSerializer>();

			builder.RegisterType<FakeTimeProvider>().As<TimeProvider>();
		});

		UpdateCheckService sut = mock.Create<UpdateCheckService>();

		// Act
		UpdateCheckResult result = await sut.CheckAsync();

		// Assert
		result.IsUpdateAvailable
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
		IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			FakeTimeProvider time = new();

			StubHttpMessageHandler handler = StubHttpMessageHandler.FromStatusCode(
				HttpStatusCode.OK,
				Releases(("v0.2.0", "https://example.test/release", false)));

			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IAppVersionProvider versionProvider = Substitute.For<IAppVersionProvider>();

			factory
				.CreateClient(Arg.Any<string>())
				.Returns(_ => new HttpClient(handler));

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					LastUpdateCheckAt = time.GetUtcNow() - TimeSpan.FromDays(2.0)
				});

			versionProvider
				.CurrentVersion
				.Returns("0.1.0");

			builder.RegisterInstance(factory);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(versionProvider);

			builder.RegisterInstance<TimeProvider>(time);

			builder.RegisterType<SystemTextJsonSerializer>().As<IJsonSerializer>();
		});

		UpdateCheckService sut = mock.Create<UpdateCheckService>();

		// Act
		UpdateCheckResult result = await sut.CheckAsync();

		// Assert
		result.IsUpdateAvailable
			.Should()
			.BeTrue();

		factory
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
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();

			StubHttpMessageHandler handler = StubHttpMessageHandler.FromStatusCode(
				HttpStatusCode.OK,
				Releases(
					("v0.3.0", "https://example.test/draft", true),
					("v0.2.0", "https://example.test/published", false)));

			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IAppVersionProvider versionProvider = Substitute.For<IAppVersionProvider>();

			factory
				.CreateClient(Arg.Any<string>())
				.Returns(_ => new HttpClient(handler));

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			versionProvider
				.CurrentVersion
				.Returns("0.1.0");

			builder.RegisterInstance(factory);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(versionProvider);

			builder.RegisterType<SystemTextJsonSerializer>().As<IJsonSerializer>();

			builder.RegisterType<FakeTimeProvider>().As<TimeProvider>();
		});

		UpdateCheckService sut = mock.Create<UpdateCheckService>();

		// Act
		UpdateCheckResult result = await sut.CheckAsync();

		// Assert
		result.IsUpdateAvailable
			.Should()
			.BeTrue();

		result.LatestVersion
			.Should()
			.Be("0.2.0");
	}

	/// <summary>
	/// <see cref="UpdateCheckService.CheckAsync" />: does not offer a version that was already offered.
	/// </summary>
	[Test]
	public async Task CheckAsync_Skips_Version_Already_Notified()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();

			StubHttpMessageHandler handler = StubHttpMessageHandler.FromStatusCode(
				HttpStatusCode.OK,
				Releases(("v0.2.0", "https://example.test/release", false)));

			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			IAppVersionProvider versionProvider = Substitute.For<IAppVersionProvider>();

			factory
				.CreateClient(Arg.Any<string>())
				.Returns(_ => new HttpClient(handler));

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					LastNotifiedVersion = "0.2.0"
				});

			versionProvider
				.CurrentVersion
				.Returns("0.1.0");

			builder.RegisterInstance(factory);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(versionProvider);

			builder.RegisterType<SystemTextJsonSerializer>().As<IJsonSerializer>();

			builder.RegisterType<FakeTimeProvider>().As<TimeProvider>();
		});

		UpdateCheckService sut = mock.Create<UpdateCheckService>();

		// Act
		UpdateCheckResult result = await sut.CheckAsync();

		// Assert
		result.IsUpdateAvailable
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="UpdateCheckService.CheckAsync" />: does nothing when the opt-out is set.
	/// </summary>
	[Test]
	public async Task CheckAsync_Skips_When_Opted_Out()
	{
		// Arrange
		IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();

		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					CheckForUpdates = false
				});

			builder.RegisterInstance(factory);

			builder.RegisterInstance(settingsStore);
		});

		UpdateCheckService sut = mock.Create<UpdateCheckService>();

		// Act
		UpdateCheckResult result = await sut.CheckAsync();

		// Assert
		result.IsUpdateAvailable
			.Should()
			.BeFalse();

		factory
			.DidNotReceive()
			.CreateClient(Arg.Any<string>());

		settingsStore
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
		IHttpClientFactory factory = Substitute.For<IHttpClientFactory>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			FakeTimeProvider time = new();

			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings() with
				{
					LastUpdateCheckAt = time.GetUtcNow() - TimeSpan.FromHours(1.0)
				});

			builder.RegisterInstance(factory);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance<TimeProvider>(time);
		});

		UpdateCheckService sut = mock.Create<UpdateCheckService>();

		// Act
		UpdateCheckResult result = await sut.CheckAsync();

		// Assert
		result.IsUpdateAvailable
			.Should()
			.BeFalse();

		factory
			.DidNotReceive()
			.CreateClient(Arg.Any<string>());
	}
	#endregion

	#region Helpers
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
}
