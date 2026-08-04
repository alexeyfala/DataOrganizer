using Baksteen.Extensions.DeepCopy;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Settings;
using DialogHostAvalonia;
using Material.Colors;
using Material.Styles.Themes.Base;
using Shared.Extensions;
using System;
using System.ComponentModel;
using System.Globalization;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>SettingsView</c>.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
	#region Properties
	/// <summary>
	/// Sequence of application languages.
	/// </summary>
	public static CultureInfo[] Languages { get; } = IAppSettingsStore.Languages;

	/// <summary>
	/// The sequence of the primary colors of the application.
	/// </summary>
	public static PrimaryColor[] PrimaryColors { get; } = Enum.GetValues<PrimaryColor>();

	/// <summary>
	/// The sequence of the accent colors of the application.
	/// </summary>
	public static SecondaryColor[] SecondaryColors { get; } = Enum.GetValues<SecondaryColor>();

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

	/// <summary>
	/// Specifies that the <see cref="BaseThemeMode.Dark" /> theme is used.
	/// </summary>
	[ObservableProperty]
	public partial bool IsDarkTheme { get; set; }

	/// <summary>
	/// <c>True</c> when the current settings differ from the saved ones.
	/// </summary>
	public bool IsDirty => !Equals(CurrentSettings, _settingsStore.Settings);

	/// <summary>
	/// <c>True</c> when the user has chosen to close the view without saving.
	/// </summary>
	public bool IsDiscarded { get; private set; }

	/// <summary>
	/// Specifies that the <see cref="BaseThemeMode.Inherit" /> theme is used.
	/// </summary>
	[ObservableProperty]
	public partial bool IsInheritTheme { get; set; }

	/// <summary>
	/// Specifies that the <see cref="BaseThemeMode.Light" /> theme is used.
	/// </summary>
	[ObservableProperty]
	public partial bool IsLightTheme { get; set; }

	/// <summary>
	/// <c>True</c> when the user has saved the settings.
	/// </summary>
	public bool IsSaved { get; private set; }

	/// <inheritdoc cref="AppSettings.Language" />
	[ObservableProperty]
	public partial CultureInfo? Language { get; set; }

	/// <inheritdoc cref="AppSettings.PersistClipboardHistory" />
	[ObservableProperty]
	public partial bool PersistClipboardHistory { get; set; }

	/// <inheritdoc cref="AppSettings.PrimaryColor" />
	[ObservableProperty]
	public partial PrimaryColor PrimaryColor { get; set; }

	/// <inheritdoc cref="AppSettings.SecondaryColor" />
	[ObservableProperty]
	public partial SecondaryColor SecondaryColor { get; set; }

	/// <summary>
	/// Index of the settings category displayed in the view.
	/// </summary>
	[ObservableProperty]
	public partial int SelectedCategoryIndex { get; set; }

	/// <inheritdoc cref="AppSettings.ShowFavoritesOnHover" />
	[ObservableProperty]
	public partial bool ShowFavoritesOnHover { get; set; }

	/// <inheritdoc cref="AppSettings.TrackClipboardHistory" />
	[ObservableProperty]
	public partial bool TrackClipboardHistory { get; set; }

	/// <inheritdoc cref="AppSettings.TrackHotkeys" />
	[ObservableProperty]
	public partial bool TrackHotkeys { get; set; }
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="CheckForUpdates" /> changes.
	/// </summary>
	partial void OnCheckForUpdatesChanged(bool value)
	{
		CurrentSettings.CheckForUpdates = value;

		SaveAndCloseCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Called when <see cref="IsDarkTheme" /> changes.
	/// </summary>
	partial void OnIsDarkThemeChanged(bool value)
	{
		if (!value)
		{
			return;
		}

		const BaseThemeMode theme = BaseThemeMode.Dark;

		CurrentSettings.Theme = theme;

		SaveAndCloseCommand.NotifyCanExecuteChanged();

		_themeService.SetAppMaterialTheme(
			theme,
			PrimaryColor,
			SecondaryColor);
	}

	/// <summary>
	/// Called when <see cref="IsInheritTheme" /> changes.
	/// </summary>
	partial void OnIsInheritThemeChanged(bool value)
	{
		if (!value)
		{
			return;
		}

		const BaseThemeMode theme = BaseThemeMode.Inherit;

		CurrentSettings.Theme = theme;

		SaveAndCloseCommand.NotifyCanExecuteChanged();

		_themeService.SetAppMaterialTheme(
			theme,
			PrimaryColor,
			SecondaryColor);
	}

	/// <summary>
	/// Called when <see cref="IsLightTheme" /> changes.
	/// </summary>
	partial void OnIsLightThemeChanged(bool value)
	{
		if (!value)
		{
			return;
		}

		const BaseThemeMode theme = BaseThemeMode.Light;

		CurrentSettings.Theme = theme;

		SaveAndCloseCommand.NotifyCanExecuteChanged();

		_themeService.SetAppMaterialTheme(
			theme,
			PrimaryColor,
			SecondaryColor);
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

		SaveAndCloseCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Called when <see cref="PersistClipboardHistory" /> changes.
	/// </summary>
	partial void OnPersistClipboardHistoryChanged(bool value)
	{
		CurrentSettings.PersistClipboardHistory = value;

		SaveAndCloseCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Called when <see cref="PrimaryColor" /> changes.
	/// </summary>
	partial void OnPrimaryColorChanged(PrimaryColor value)
	{
		CurrentSettings.PrimaryColor = value;

		SaveAndCloseCommand.NotifyCanExecuteChanged();

		_themeService.SetAppMaterialTheme(
			CurrentSettings.Theme,
			value,
			SecondaryColor);
	}

	/// <summary>
	/// Called when <see cref="SecondaryColor" /> changes.
	/// </summary>
	partial void OnSecondaryColorChanged(SecondaryColor value)
	{
		CurrentSettings.SecondaryColor = value;

		SaveAndCloseCommand.NotifyCanExecuteChanged();

		_themeService.SetAppMaterialTheme(
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

		SaveAndCloseCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Called when <see cref="TrackClipboardHistory" /> changes.
	/// </summary>
	partial void OnTrackClipboardHistoryChanged(bool value)
	{
		CurrentSettings.TrackClipboardHistory = value;

		SaveAndCloseCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Called when <see cref="TrackHotkeys" /> changes.
	/// </summary>
	partial void OnTrackHotkeysChanged(bool value)
	{
		CurrentSettings.TrackHotkeys = value;

		SaveAndCloseCommand.NotifyCanExecuteChanged();
	}
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Closes the view without saving the changes.
	/// </summary>
	[RelayCommand]
	internal void DiscardAndClose()
	{
		IsDiscarded = true;

		IsConfirmingClose = false;

		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		DialogHost.Close(null);
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
		AppSettings defaults = CreateDefaults();

		CheckForUpdates = defaults.CheckForUpdates;

		TrackClipboardHistory = defaults.TrackClipboardHistory;

		PersistClipboardHistory = defaults.PersistClipboardHistory;

		TrackHotkeys = defaults.TrackHotkeys;

		ShowFavoritesOnHover = defaults.ShowFavoritesOnHover;

		SecondaryColor = defaults.SecondaryColor;

		PrimaryColor = defaults.PrimaryColor;

		Language = new(defaults.Language);

		// A handler of a theme flag acts on the selected one only, so the order of the assignments does not matter.
		IsLightTheme = defaults.Theme == BaseThemeMode.Light;

		IsInheritTheme = defaults.Theme == BaseThemeMode.Inherit;

		IsDarkTheme = defaults.Theme == BaseThemeMode.Dark;
	}

	/// <summary>
	/// Saves settings and closes the view.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanSaveAndClose))]
	internal void SaveAndClose()
	{
		IsSaved = true;

		IsConfirmingClose = false;

		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		DialogHost.Close(null);
	}
	#endregion

	#region Data
	/// <inheritdoc cref="ISettingsSessionState" />
	private readonly ISettingsSessionState _sessionState;

	/// <inheritdoc cref="IAppSettingsStore" />
	private readonly IAppSettingsStore _settingsStore;

	/// <inheritdoc cref="IAppThemeService" />
	private readonly IAppThemeService _themeService;
	#endregion

	#region Constructors
	public SettingsViewModel(
		IAppSettingsStore settingsStore,
		IAppThemeService themeService,
		ISettingsSessionState sessionState)
	{
		_sessionState = sessionState;

		_settingsStore = settingsStore;

		_themeService = themeService;

		SelectedCategoryIndex = sessionState.LastCategoryIndex;

		CurrentSettings = settingsStore.Settings.DeepCopy() ?? IAppSettingsStore.CreateDefaultSettings();

		CheckForUpdates = CurrentSettings.CheckForUpdates;

		TrackClipboardHistory = CurrentSettings.TrackClipboardHistory;

		PersistClipboardHistory = CurrentSettings.PersistClipboardHistory;

		TrackHotkeys = CurrentSettings.TrackHotkeys;

		ShowFavoritesOnHover = CurrentSettings.ShowFavoritesOnHover;

		SecondaryColor = CurrentSettings.SecondaryColor;

		PrimaryColor = CurrentSettings.PrimaryColor;

		Language = new(CurrentSettings.Language);

		IsLightTheme = CurrentSettings.Theme == BaseThemeMode.Light;

		IsInheritTheme = CurrentSettings.Theme == BaseThemeMode.Inherit;

		IsDarkTheme = CurrentSettings.Theme == BaseThemeMode.Dark;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		RestoreDefaultSettingsCommand.NotifyCanExecuteChanged();
	}
	#endregion

	#region Helpers
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

		defaults.LastUpdateCheckUtc = CurrentSettings.LastUpdateCheckUtc;

		return defaults;
	}
	#endregion
}
