using AppUpdates.Dto;
using AppUpdates.Interfaces;
using AppUpdates.Services;
using Autofac;
using Autofac.Extras.Moq;
using FileExecution.Interfaces;
using NSubstitute;
using System.Threading;
using System.Threading.Tasks;

namespace AppUpdates.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(UpdateNotifier)}"" type")]
internal class UpdateNotifierTests
{
	#region Data
	private const string ReleaseUrl = "https://example.test/release";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="UpdateNotifier.NotifyIfUpdateAvailableAsync" />: does not open the release page when the user declines.
	/// </summary>
	[Test]
	public async Task NotifyIfUpdateAvailableAsync_Does_Not_Open_When_User_Declines()
	{
		// Arrange
		UpdateCheckResult result = new()
		{
			IsUpdateAvailable = true,
			LatestVersion = "0.2.0",
			ReleaseUrl = ReleaseUrl
		};

		IUpdatePrompt prompt = Substitute.For<IUpdatePrompt>();

		IProcessManager processManager = Substitute.For<IProcessManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IUpdateCheckService updateCheckService = Substitute.For<IUpdateCheckService>();

			updateCheckService
				.CheckAsync(Arg.Any<CancellationToken>())
				.Returns(result);

			prompt
				.ConfirmUpdateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(updateCheckService);

			builder.RegisterInstance(processManager);

			builder.RegisterInstance(prompt);
		});

		UpdateNotifier sut = mock.Create<UpdateNotifier>();

		// Act
		await sut.NotifyIfUpdateAvailableAsync();

		// Assert
		await prompt
			.Received(1)
			.ConfirmUpdateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

		processManager
			.DidNotReceive()
			.StartProcess(Arg.Any<string>(), out _);
	}

	/// <summary>
	/// <see cref="UpdateNotifier.NotifyIfUpdateAvailableAsync" />: shows no prompt for a release that is no update, even
	/// with its URL.
	/// </summary>
	[Test]
	public async Task NotifyIfUpdateAvailableAsync_Does_Nothing_For_A_Release_That_Is_No_Update()
	{
		// Arrange
		UpdateCheckResult result = new()
		{
			IsUpdateAvailable = false,
			LatestVersion = "0.2.0",
			ReleaseUrl = ReleaseUrl
		};

		IUpdatePrompt prompt = Substitute.For<IUpdatePrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IUpdateCheckService updateCheckService = Substitute.For<IUpdateCheckService>();

			updateCheckService
				.CheckAsync(Arg.Any<CancellationToken>())
				.Returns(result);

			builder.RegisterInstance(updateCheckService);

			builder.RegisterInstance(prompt);
		});

		UpdateNotifier sut = mock.Create<UpdateNotifier>();

		// Act
		await sut.NotifyIfUpdateAvailableAsync();

		// Assert
		await prompt
			.DidNotReceive()
			.ConfirmUpdateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="UpdateNotifier.NotifyIfUpdateAvailableAsync" />: shows no prompt when no update is available.
	/// </summary>
	[Test]
	public async Task NotifyIfUpdateAvailableAsync_Does_Nothing_When_No_Update()
	{
		// Arrange
		IUpdatePrompt prompt = Substitute.For<IUpdatePrompt>();

		IProcessManager processManager = Substitute.For<IProcessManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IUpdateCheckService updateCheckService = Substitute.For<IUpdateCheckService>();

			updateCheckService
				.CheckAsync(Arg.Any<CancellationToken>())
				.Returns(UpdateCheckResult.None);

			builder.RegisterInstance(updateCheckService);

			builder.RegisterInstance(processManager);

			builder.RegisterInstance(prompt);
		});

		UpdateNotifier sut = mock.Create<UpdateNotifier>();

		// Act
		await sut.NotifyIfUpdateAvailableAsync();

		// Assert
		await prompt
			.DidNotReceive()
			.ConfirmUpdateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

		processManager
			.DidNotReceive()
			.StartProcess(Arg.Any<string>(), out _);
	}

	/// <summary>
	/// <see cref="UpdateNotifier.NotifyIfUpdateAvailableAsync" />: shows no prompt when the release URL is not secure.
	/// </summary>
	[Test]
	public async Task NotifyIfUpdateAvailableAsync_Does_Nothing_When_Release_Url_Is_Not_Secure()
	{
		// Arrange
		UpdateCheckResult result = new()
		{
			IsUpdateAvailable = true,
			LatestVersion = "0.2.0",
			ReleaseUrl = "http://example.test/release"
		};

		IUpdatePrompt prompt = Substitute.For<IUpdatePrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IUpdateCheckService updateCheckService = Substitute.For<IUpdateCheckService>();

			updateCheckService
				.CheckAsync(Arg.Any<CancellationToken>())
				.Returns(result);

			builder.RegisterInstance(updateCheckService);

			builder.RegisterInstance(prompt);
		});

		UpdateNotifier sut = mock.Create<UpdateNotifier>();

		// Act
		await sut.NotifyIfUpdateAvailableAsync();

		// Assert
		await prompt
			.DidNotReceive()
			.ConfirmUpdateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="UpdateNotifier.NotifyIfUpdateAvailableAsync" />: shows no prompt when the release URL is missing.
	/// </summary>
	[Test]
	public async Task NotifyIfUpdateAvailableAsync_Does_Nothing_When_Release_Url_Missing()
	{
		// Arrange
		UpdateCheckResult result = new()
		{
			IsUpdateAvailable = true,
			LatestVersion = "0.2.0",
			ReleaseUrl = null
		};

		IUpdatePrompt prompt = Substitute.For<IUpdatePrompt>();

		IProcessManager processManager = Substitute.For<IProcessManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IUpdateCheckService updateCheckService = Substitute.For<IUpdateCheckService>();

			updateCheckService
				.CheckAsync(Arg.Any<CancellationToken>())
				.Returns(result);

			builder.RegisterInstance(updateCheckService);

			builder.RegisterInstance(processManager);

			builder.RegisterInstance(prompt);
		});

		UpdateNotifier sut = mock.Create<UpdateNotifier>();

		// Act
		await sut.NotifyIfUpdateAvailableAsync();

		// Assert
		await prompt
			.DidNotReceive()
			.ConfirmUpdateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

		processManager
			.DidNotReceive()
			.StartProcess(Arg.Any<string>(), out _);
	}

	/// <summary>
	/// <see cref="UpdateNotifier.NotifyIfUpdateAvailableAsync" />: opens the release page when the user accepts.
	/// </summary>
	[Test]
	public async Task NotifyIfUpdateAvailableAsync_Opens_Release_Page_When_User_Accepts()
	{
		// Arrange
		UpdateCheckResult result = new()
		{
			IsUpdateAvailable = true,
			LatestVersion = "0.2.0",
			ReleaseUrl = ReleaseUrl
		};

		IUpdatePrompt prompt = Substitute.For<IUpdatePrompt>();

		IProcessManager processManager = Substitute.For<IProcessManager>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IUpdateCheckService updateCheckService = Substitute.For<IUpdateCheckService>();

			updateCheckService
				.CheckAsync(Arg.Any<CancellationToken>())
				.Returns(result);

			prompt
				.ConfirmUpdateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(updateCheckService);

			builder.RegisterInstance(processManager);

			builder.RegisterInstance(prompt);
		});

		UpdateNotifier sut = mock.Create<UpdateNotifier>();

		// Act
		await sut.NotifyIfUpdateAvailableAsync();

		// Assert
		await prompt
			.Received(1)
			.ConfirmUpdateAsync(Arg.Is<string>(static x => x != null && x.Contains("0.2.0")), Arg.Any<CancellationToken>());

		processManager
			.Received(1)
			.StartProcess(ReleaseUrl, out _);
	}
	#endregion
}
