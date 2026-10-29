using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using NSubstitute;
using Settings.Dto;
using Settings.Enums;
using Settings.Interfaces;
using Settings.Services;
using Settings.UnitTests.Factories;
using Shared.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace Settings.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(SettingsSession)}"" type")]
internal class SettingsSessionTests
{
	#region Methods
	/// <summary>
	/// <see cref="SettingsSession.Begin" />: a new session starts neither saved nor discarded, whatever the last one ended
	/// with.
	/// </summary>
	[Test]
	public void Begin_Starts_Neither_Saved_Nor_Discarded()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			builder.RegisterInstance(settingsStore);
		});

		SettingsSession sut = mock.Create<SettingsSession>();

		sut.Begin();

		sut.Save();

		sut.Discard();

		// Act
		sut.Begin();

		// Assert
		bool[] flags = [sut.IsSaved, sut.IsDiscarded];

		flags
			.Should()
			.OnlyContain(static x => !x);
	}

	/// <summary>
	/// <see cref="SettingsSession.EndAsync" />: saved settings are written and handed to the applier together with what the
	/// replaced settings had on.
	/// </summary>
	[Test]
	public async Task EndAsync_Hands_Saved_Settings_To_The_Applier_With_The_Replaced_Values()
	{
		// Arrange
		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		ISettingsApplier settingsApplier = Substitute.For<ISettingsApplier>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			AppSettings stored = SettingsFactory.CreateSettings(autoLockMinutes: 3);

			stored.PersistClipboardHistory = true;

			settingsStore
				.Settings
				.Returns(stored);

			// The store overwrites its settings in place, as the real one does.
			settingsStore
				.When(x => x.Overwrite(Arg.Any<AppSettings>()))
				.Do(x => x.Arg<AppSettings>().CopyPropertiesTo(stored));

			builder.RegisterInstance(settingsApplier);

			builder.RegisterInstance(settingsStore);
		});

		SettingsSession sut = mock.Create<SettingsSession>();

		AppSettings draft = sut.Begin();

		draft.AutoLockMinutes = 5;

		draft.PersistClipboardHistory = false;

		sut.Save();

		// Act
		AppSettings? result = await sut.EndAsync();

		// Assert
		result
			.Should()
			.BeSameAs(draft);

		settingsStore
			.Received(1)
			.Overwrite(draft);

		settingsStore
			.Received(1)
			.Save();

		await settingsApplier.Received(1).ApplySettingsAsync(
			draft,
			wasPersistingClipboard: true,
			previousAutoLockMinutes: 3,
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="SettingsSession.EndAsync" />: settings closed without saving roll the previewed theme back and are neither
	/// written nor applied.
	/// </summary>
	[Test]
	public async Task EndAsync_Rolls_The_Theme_Back_Without_A_Save()
	{
		// Arrange
		IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

		IAppThemeService themeService = Substitute.For<IAppThemeService>();

		ISettingsApplier settingsApplier = Substitute.For<ISettingsApplier>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			builder.RegisterInstance(settingsApplier);

			builder.RegisterInstance(settingsStore);

			builder.RegisterInstance(themeService);
		});

		SettingsSession sut = mock.Create<SettingsSession>();

		sut.Begin();

		// Act
		AppSettings? result = await sut.EndAsync();

		// Assert
		result
			.Should()
			.BeNull();

		themeService
			.Received(1)
			.ApplyFromSettings();

		settingsStore
			.DidNotReceive()
			.Save();

		await settingsApplier
			.DidNotReceiveWithAnyArgs()
			.ApplySettingsAsync(default!, default, default);
	}

	/// <summary>
	/// <see cref="SettingsSession.PreviewTheme" />: the chosen theme shows at once, before the settings are saved.
	/// </summary>
	[Test]
	public void PreviewTheme_Shows_The_Theme_At_Once()
	{
		// Arrange
		IAppThemeService themeService = Substitute.For<IAppThemeService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(themeService);
		});

		SettingsSession sut = mock.Create<SettingsSession>();

		// Act
		sut.PreviewTheme(ThemeMode.Dark, ThemePrimaryColor.Purple, ThemeSecondaryColor.Lime);

		// Assert
		themeService
			.Received(1)
			.SetTheme(ThemeMode.Dark, ThemePrimaryColor.Purple, ThemeSecondaryColor.Lime);
	}

	/// <summary>
	/// <see cref="SettingsSession.ShouldConfirmClose" />: edits that are neither saved nor discarded ask for a confirmation,
	/// saved or discarded ones and unchanged settings do not.
	/// </summary>
	[Test]
	[TestCase(true, false, false, ExpectedResult = true)]
	[TestCase(true, true, false, ExpectedResult = false)]
	[TestCase(true, false, true, ExpectedResult = false)]
	[TestCase(false, false, false, ExpectedResult = false)]
	public bool ShouldConfirmClose_Asks_Only_For_Pending_Edits(bool isEdited, bool isSaved, bool isDiscarded)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IAppSettingsStore settingsStore = Substitute.For<IAppSettingsStore>();

			settingsStore
				.Settings
				.Returns(SettingsFactory.CreateSettings());

			builder.RegisterInstance(settingsStore);
		});

		SettingsSession sut = mock.Create<SettingsSession>();

		AppSettings draft = sut.Begin();

		if (isEdited)
		{
			draft.CheckForUpdates = !draft.CheckForUpdates;
		}

		if (isSaved)
		{
			sut.Save();
		}

		if (isDiscarded)
		{
			sut.Discard();
		}

		// Act
		return sut.ShouldConfirmClose;
	}
	#endregion
}
