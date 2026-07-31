using DataOrganizer.DTO.Updates;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Updates;
using DataOrganizer.Services.Updates;
using NSubstitute;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.TestTypes;

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
			UpdateAvailable = true,
			LatestVersion = "0.2.0",
			ReleaseUrl = ReleaseUrl
		};

		Context context = CreateContext(result, promptAnswer: false);

		// Act
		await context
			.Sut
			.NotifyIfUpdateAvailableAsync(context.Prompt);

		// Assert
		await context.Prompt
			.Received(1)
			.ConfirmUpdateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

		context.ProcessUtils
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
		Context context = CreateContext(UpdateCheckResult.None, promptAnswer: false);

		// Act
		await context
			.Sut
			.NotifyIfUpdateAvailableAsync(context.Prompt);

		// Assert
		await context.Prompt
			.DidNotReceive()
			.ConfirmUpdateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

		context.ProcessUtils
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
			UpdateAvailable = true,
			LatestVersion = "0.2.0",
			ReleaseUrl = null
		};

		Context context = CreateContext(result, promptAnswer: true);

		// Act
		await context
			.Sut
			.NotifyIfUpdateAvailableAsync(context.Prompt);

		// Assert
		await context.Prompt
			.DidNotReceive()
			.ConfirmUpdateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

		context.ProcessUtils
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
			UpdateAvailable = true,
			LatestVersion = "0.2.0",
			ReleaseUrl = ReleaseUrl
		};

		Context context = CreateContext(result, promptAnswer: true);

		// Act
		await context
			.Sut
			.NotifyIfUpdateAvailableAsync(context.Prompt);

		// Assert
		await context.Prompt
			.Received(1)
			.ConfirmUpdateAsync(Arg.Is<string>(static x => x != null && x.Contains("0.2.0")), Arg.Any<CancellationToken>());

		context.ProcessUtils
			.Received(1)
			.StartProcess(ReleaseUrl, out _);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds a notifier under test wired with a canned check result and prompt answer.
	/// </summary>
	private static Context CreateContext(UpdateCheckResult checkResult, bool promptAnswer)
	{
		IUpdateCheckService updateCheckService = Substitute.For<IUpdateCheckService>();

		updateCheckService
			.CheckAsync(Arg.Any<CancellationToken>())
			.Returns(checkResult);

		IUpdatePrompt prompt = Substitute.For<IUpdatePrompt>();

		prompt
			.ConfirmUpdateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(promptAnswer);

		IProcessUtils processUtils = Substitute.For<IProcessUtils>();

		return new Context
		{
			Prompt = prompt,
			ProcessUtils = processUtils,
			Sut = new UpdateNotifier(processUtils, updateCheckService)
		};
	}
	#endregion

	#region Nested Types
	/// <summary>
	/// Bundles the notifier under test with its captured collaborators.
	/// </summary>
	private sealed class Context
	{
		#region Properties
		public required IProcessUtils ProcessUtils { get; init; }
		public required IUpdatePrompt Prompt { get; init; }
		public required UpdateNotifier Sut { get; init; }
		#endregion
	}
	#endregion
}
