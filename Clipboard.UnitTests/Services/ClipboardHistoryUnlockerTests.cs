using Autofac;
using Autofac.Extras.Moq;
using Clipboard.Enums;
using Clipboard.Interfaces;
using Clipboard.Services;
using DataProtection.Enums;
using DataProtection.Helpers;
using DataProtection.Interfaces;
using DataProtection.UnitTests.Factories;
using NSubstitute;
using System.Threading;
using System.Threading.Tasks;

namespace Clipboard.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardHistoryUnlocker)}"" type")]
internal class ClipboardHistoryUnlockerTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClipboardHistoryUnlocker.UnlockIfRequiredAsync" />: a wrong password is asked for again, until the history
	/// opens.
	/// </summary>
	[Test]
	public async Task UnlockIfRequiredAsync_Asks_Again_After_A_Wrong_Password()
	{
		// Arrange
		IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogPersistenceCoordinator persistence = Substitute.For<IClipboardLogPersistenceCoordinator>();

			persistence
				.RequiresUnlock
				.Returns(true);

			persistence
				.HasPassword
				.Returns(true);

			persistence
				.TryUnlockAndMergeAsync(Arg.Any<PinnedBuffer>(), Arg.Any<CancellationToken>())
				.Returns(ClipboardLogStatus.WrongPassword, ClipboardLogStatus.Unlocked);

			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(_ => SecretFactory.CreateRandomSecret());

			builder.RegisterInstance(persistence);

			builder.RegisterInstance(passwordPrompt);
		});

		ClipboardHistoryUnlocker sut = mock.Create<ClipboardHistoryUnlocker>();

		// Act
		await sut.UnlockIfRequiredAsync();

		// Assert
		await passwordPrompt.Received(2).RequestPasswordAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<PasswordPromptMode>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ClipboardHistoryUnlocker.UnlockIfRequiredAsync" />: the first password of the saved history is created
	/// with a confirmation, a later one is only checked.
	/// </summary>
	[TestCase(false, PasswordPromptMode.Create)]
	[TestCase(true, PasswordPromptMode.Verify)]
	public async Task UnlockIfRequiredAsync_Asks_For_The_History_Password(bool hasPassword, PasswordPromptMode expected)
	{
		// Arrange
		IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogPersistenceCoordinator persistence = Substitute.For<IClipboardLogPersistenceCoordinator>();

			persistence
				.RequiresUnlock
				.Returns(true);

			persistence
				.HasPassword
				.Returns(hasPassword);

			// A cancelled prompt leaves the session in memory, which is enough to reach the assert.
			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(new PinnedSecret(length: 0));

			builder.RegisterInstance(persistence);

			builder.RegisterInstance(passwordPrompt);
		});

		ClipboardHistoryUnlocker sut = mock.Create<ClipboardHistoryUnlocker>();

		// Act
		await sut.UnlockIfRequiredAsync();

		// Assert
		await passwordPrompt.Received(1).RequestPasswordAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<string>(),
			expected,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ClipboardHistoryUnlocker.UnlockIfRequiredAsync" />: a history that is not locked asks for no password.
	/// </summary>
	[Test]
	public async Task UnlockIfRequiredAsync_Asks_Nothing_Without_A_Locked_History()
	{
		// Arrange
		IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(passwordPrompt));

		ClipboardHistoryUnlocker sut = mock.Create<ClipboardHistoryUnlocker>();

		// Act
		await sut.UnlockIfRequiredAsync();

		// Assert
		await passwordPrompt.DidNotReceive().RequestPasswordAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<PasswordPromptMode>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ClipboardHistoryUnlocker.UnlockIfRequiredAsync" />: a damaged history is not asked for again, as no
	/// password fixes it.
	/// </summary>
	[Test]
	public async Task UnlockIfRequiredAsync_Stops_Asking_For_A_Damaged_History()
	{
		// Arrange
		IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IClipboardLogPersistenceCoordinator persistence = Substitute.For<IClipboardLogPersistenceCoordinator>();

			persistence
				.RequiresUnlock
				.Returns(true);

			persistence
				.HasPassword
				.Returns(true);

			persistence
				.TryUnlockAndMergeAsync(Arg.Any<PinnedBuffer>(), Arg.Any<CancellationToken>())
				.Returns(ClipboardLogStatus.Damaged, ClipboardLogStatus.Unlocked);

			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(_ => SecretFactory.CreateRandomSecret());

			builder.RegisterInstance(persistence);

			builder.RegisterInstance(passwordPrompt);
		});

		ClipboardHistoryUnlocker sut = mock.Create<ClipboardHistoryUnlocker>();

		// Act
		await sut.UnlockIfRequiredAsync();

		// Assert
		await passwordPrompt.Received(1).RequestPasswordAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<PasswordPromptMode>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="ClipboardHistoryUnlocker.UnlockIfRequiredAsync" />: a cancelled prompt tries no unlock, so no history
	/// key is made with an empty password.
	/// </summary>
	[Test]
	public async Task UnlockIfRequiredAsync_Tries_No_Unlock_When_The_Prompt_Is_Cancelled()
	{
		// Arrange
		IClipboardLogPersistenceCoordinator persistence = Substitute.For<IClipboardLogPersistenceCoordinator>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IPasswordPrompt passwordPrompt = Substitute.For<IPasswordPrompt>();

			persistence
				.RequiresUnlock
				.Returns(true);

			// The history has no password yet, so an unlock would create its key.
			persistence
				.HasPassword
				.Returns(false);

			passwordPrompt
				.RequestPasswordAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(new PinnedSecret(length: 0));

			builder.RegisterInstance(persistence);

			builder.RegisterInstance(passwordPrompt);
		});

		ClipboardHistoryUnlocker sut = mock.Create<ClipboardHistoryUnlocker>();

		// Act
		await sut.UnlockIfRequiredAsync();

		// Assert
		await persistence
			.DidNotReceive()
			.TryUnlockAndMergeAsync(Arg.Any<PinnedBuffer>(), Arg.Any<CancellationToken>());
	}
	#endregion
}
