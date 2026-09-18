using Autofac;
using Autofac.Extras.Moq;
using DataOrganizer.Dto.Updates;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Updates;
using DataOrganizer.Services.Updates;
using NSubstitute;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Services.Updates;

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
		});

		UpdateNotifier sut = mock.Create<UpdateNotifier>();

		// Act
		await sut.NotifyIfUpdateAvailableAsync(prompt);

		// Assert
		await prompt
			.Received(1)
			.ConfirmUpdateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

		processManager
			.DidNotReceive()
			.StartProcess(Arg.Any<string>(), out _);
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
		});

		UpdateNotifier sut = mock.Create<UpdateNotifier>();

		// Act
		await sut.NotifyIfUpdateAvailableAsync(prompt);

		// Assert
		await prompt
			.DidNotReceive()
			.ConfirmUpdateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

		processManager
			.DidNotReceive()
			.StartProcess(Arg.Any<string>(), out _);
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
		});

		UpdateNotifier sut = mock.Create<UpdateNotifier>();

		// Act
		await sut.NotifyIfUpdateAvailableAsync(prompt);

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
		});

		UpdateNotifier sut = mock.Create<UpdateNotifier>();

		// Act
		await sut.NotifyIfUpdateAvailableAsync(prompt);

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
