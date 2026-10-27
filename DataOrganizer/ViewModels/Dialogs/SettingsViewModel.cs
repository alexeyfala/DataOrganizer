using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Interfaces.Dialogs;
using Settings.Dto;
using Settings.Enums;
using Settings.Interfaces;
using System;
using System.Globalization;

namespace DataOrganizer.ViewModels.Dialogs;

/// <summary>
/// View model for <c>SettingsView</c>.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
	#region Properties
	/// <summary>
	/// Selectable auto-lock delays in minutes, where <c>0</c> stands for no auto-lock.
	/// </summary>
	public static int[] AutoLockDelays { get; } = [0, 1, 3, 5, 10, 15, 30, 60];

	/// <summary>
	/// Sequence of application languages.
	/// </summary>
	public static CultureInfo[] Languages { get; } = IAppSettingsStore.Languages;

	/// <summary>
	/// The sequence of the primary colors of the application.
	/// </summary>
	public static ThemePrimaryColor[] PrimaryColors { get; } = Enum.GetValues<ThemePrimaryColor>();

	/// <summary>
	/// The sequence of the accent colors of the application.
	/// </summary>
	public static ThemeSecondaryColor[] SecondaryColors { get; } = Enum.GetValues<ThemeSecondaryColor>();

	/// <inheritdoc cref="AppSettings.AutoLockMinutes" />
	[ObservableProperty]
	public partial int AutoLockMinutes { get; set; }

	/// <inheritdoc cref="AppSettings.CheckForUpdates" />
	[ObservableProperty]
	public partial bool CheckForUpdates { get; set; }

	/// <summary>
	/// Current settings for user to change.
	/// </summary>
	public AppSettings CurrentSettings { get; }

	/// <summary>
	/// <c>True</c> when the confirmation of closing with unsaved changes is displayed.
	/// </summary>
	[ObservableProperty]
	public partial bool IsConfirmingClose { get; set; }

	/// <inheritdoc cref="ISettingsDraft.IsDirty" />
	public bool IsDirty => _settingsDraft.IsDirty;

	/// <inheritdoc cref="ISettingsDraft.IsDiscarded" />
	public bool IsDiscarded => _settingsDraft.IsDiscarded;

	/// <inheritdoc cref="ISettingsDraft.IsSaved" />
	public bool IsSaved => _settingsDraft.IsSaved;

	/// <inheritdoc cref="AppSettings.Language" />
	[ObservableProperty]
	public partial CultureInfo? Language { get; set; }

	/// <inheritdoc cref="AppSettings.PersistClipboardHistory" />
	[ObservableProperty]
	public partial bool PersistClipboardHistory { get; set; }

	/// <inheritdoc cref="AppSettings.PrimaryColor" />
	[ObservableProperty]
	public partial ThemePrimaryColor PrimaryColor { get; set; }

	/// <inheritdoc cref="AppSettings.SecondaryColor" />
	[ObservableProperty]
	public partial ThemeSecondaryColor SecondaryColor { get; set; }

	/// <summary>
	/// Index of the settings category displayed in the view.
	/// </summary>
	[ObservableProperty]
	public partial int SelectedCategoryIndex { get; set; }

	/// <inheritdoc cref="AppSettings.ShowFavoritesOnHover" />
	[ObservableProperty]
	public partial bool ShowFavoritesOnHover { get; set; }

	/// <inheritdoc cref="AppSettings.Theme" />
	[ObservableProperty]
	public partial ThemeMode Theme { get; set; }

	/// <inheritdoc cref="AppSettings.TrackClipboardHistory" />
	[ObservableProperty]
	public partial bool TrackClipboardHistory { get; set; }

	/// <inheritdoc cref="AppSettings.TrackHotkeys" />
	[ObservableProperty]
	public partial bool TrackHotkeys { get; set; }
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="AutoLockMinutes" /> changes.
	/// </summary>
	partial void OnAutoLockMinutesChanged(int value)
	{
		CurrentSettings.AutoLockMinutes = value;

		NotifyCommandsCanExecuteChanged();
	}

	/// <summary>
	/// Called when <see cref="CheckForUpdates" /> changes.
	/// </summary>
	partial void OnCheckForUpdatesChanged(bool value)
	{
		CurrentSettings.CheckForUpdates = value;

		NotifyCommandsCanExecuteChanged();
	}

	/// <summary>
	/// Called when <see cref="Language" /> changes.
	/// </summary>
	partial void OnLanguageChanged(CultureInfo? value)
	{
		if (value is null)
		{
			return;
		}

		CurrentSettings.Language = value.Name;

		NotifyCommandsCanExecuteChanged();
	}

	/// <summary>
	/// Called when <see cref="PersistClipboardHistory" /> changes.
	/// </summary>
	partial void OnPersistClipboardHistoryChanged(bool value)
	{
		CurrentSettings.PersistClipboardHistory = value;

		NotifyCommandsCanExecuteChanged();
	}

	/// <summary>
	/// Called when <see cref="PrimaryColor" /> changes.
	/// </summary>
	partial void OnPrimaryColorChanged(ThemePrimaryColor value)
	{
		CurrentSettings.PrimaryColor = value;

		NotifyCommandsCanExecuteChanged();

		_settingsDraft.PreviewTheme(
			CurrentSettings.Theme,
			value,
			SecondaryColor);
	}

	/// <summary>
	/// Called when <see cref="SecondaryColor" /> changes.
	/// </summary>
	partial void OnSecondaryColorChanged(ThemeSecondaryColor value)
	{
		CurrentSettings.SecondaryColor = value;

		NotifyCommandsCanExecuteChanged();

		_settingsDraft.PreviewTheme(
			CurrentSettings.Theme,
			PrimaryColor,
			value);
	}

	/// <summary>
	/// Called when <see cref="SelectedCategoryIndex" /> changes.
	/// </summary>
	partial void OnSelectedCategoryIndexChanged(int value) => _sessionState.LastCategoryIndex = value;

	/// <summary>
	/// Called when <see cref="ShowFavoritesOnHover" /> changes.
	/// </summary>
	partial void OnShowFavoritesOnHoverChanged(bool value)
	{
		CurrentSettings.ShowFavoritesOnHover = value;

		NotifyCommandsCanExecuteChanged();
	}

	/// <summary>
	/// Called when <see cref="Theme" /> changes.
	/// </summary>
	partial void OnThemeChanged(ThemeMode value)
	{
		CurrentSettings.Theme = value;

		NotifyCommandsCanExecuteChanged();

		_settingsDraft.PreviewTheme(
			value,
			PrimaryColor,
			SecondaryColor);
	}

	/// <summary>
	/// Called when <see cref="TrackClipboardHistory" /> changes.
	/// </summary>
	partial void OnTrackClipboardHistoryChanged(bool value)
	{
		CurrentSettings.TrackClipboardHistory = value;

		NotifyCommandsCanExecuteChanged();
	}

	/// <summary>
	/// Called when <see cref="TrackHotkeys" /> changes.
	/// </summary>
	partial void OnTrackHotkeysChanged(bool value)
	{
		CurrentSettings.TrackHotkeys = value;

		NotifyCommandsCanExecuteChanged();
	}
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Closes the view without saving the changes.
	/// </summary>
	[RelayCommand]
	internal void DiscardAndClose()
	{
		_settingsDraft.Discard();

		IsConfirmingClose = false;

		_dialogHostCloser.Close();
	}

	/// <summary>
	/// Dismisses the confirmation and returns to editing the settings.
	/// </summary>
	[RelayCommand]
	internal void KeepEditing() => IsConfirmingClose = false;

	/// <summary>
	/// Fills the view with the default values, leaving them unsaved.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanRestoreDefaultSettings))]
	internal void RestoreDefaultSettings()
	{
		ApplySettings(CreateDefaults());
	}

	/// <summary>
	/// Saves settings and closes the view.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanSaveAndClose))]
	internal void SaveAndClose()
	{
		_settingsDraft.Save();

		IsConfirmingClose = false;

		_dialogHostCloser.Close();
	}

	/// <summary>
	/// Closes the dialog, leaving the closing handler to intercept the unsaved changes.
	/// </summary>
	[RelayCommand]
	private void Close() => _dialogHostCloser.Close();
	#endregion

	#region Data
	/// <inheritdoc cref="IDialogHostCloser" />
	private readonly IDialogHostCloser _dialogHostCloser;

	/// <inheritdoc cref="ISettingsSessionState" />
	private readonly ISettingsSessionState _sessionState;

	/// <inheritdoc cref="ISettingsDraft" />
	private readonly ISettingsDraft _settingsDraft;
	#endregion

	#region Constructors
	public SettingsViewModel(
		IDialogHostCloser dialogHostCloser,
		ISettingsDraft settingsDraft,
		ISettingsSessionState sessionState)
	{
		_dialogHostCloser = dialogHostCloser;

		_sessionState = sessionState;

		_settingsDraft = settingsDraft;

		SelectedCategoryIndex = sessionState.LastCategoryIndex;

		CurrentSettings = settingsDraft.Begin();

		ApplySettings(CurrentSettings);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Shows the values of the settings in the properties of the view.
	/// </summary>
	private void ApplySettings(AppSettings settings)
	{
		CheckForUpdates = settings.CheckForUpdates;

		AutoLockMinutes = settings.AutoLockMinutes;

		TrackClipboardHistory = settings.TrackClipboardHistory;

		PersistClipboardHistory = settings.PersistClipboardHistory;

		TrackHotkeys = settings.TrackHotkeys;

		ShowFavoritesOnHover = settings.ShowFavoritesOnHover;

		SecondaryColor = settings.SecondaryColor;

		PrimaryColor = settings.PrimaryColor;

		Language = new(settings.Language);

		Theme = settings.Theme;
	}

	/// <summary>
	/// Validates <see cref="RestoreDefaultSettingsCommand" />.
	/// </summary>
	private bool CanRestoreDefaultSettings() => !Equals(CurrentSettings, CreateDefaults());

	/// <summary>
	/// Validates <see cref="SaveAndCloseCommand" />.
	/// </summary>
	private bool CanSaveAndClose() => IsDirty;

	/// <summary>
	/// Builds the default settings, keeping the values that the view does not display.
	/// </summary>
	private AppSettings CreateDefaults()
	{
		AppSettings defaults = IAppSettingsStore.CreateDefaultSettings();

		defaults.LastNotifiedVersion = CurrentSettings.LastNotifiedVersion;

		defaults.LastUpdateCheckAt = CurrentSettings.LastUpdateCheckAt;

		return defaults;
	}

	/// <summary>
	/// Refreshes the commands validated against the edited settings.
	/// </summary>
	private void NotifyCommandsCanExecuteChanged()
	{
		SaveAndCloseCommand.NotifyCanExecuteChanged();

		RestoreDefaultSettingsCommand.NotifyCanExecuteChanged();
	}
	#endregion
}
