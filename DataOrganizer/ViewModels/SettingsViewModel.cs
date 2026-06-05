using Baksteen.Extensions.DeepCopy;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces;
using DialogHostAvalonia;
using Material.Colors;
using Material.Styles.Themes.Base;
using Shared.Extensions;
using System;
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
	public static CultureInfo[] Languages { get; } = IAppSettingsManager.Languages;

	/// <summary>
	/// The sequence of the primary colors of the application.
	/// </summary>
	public static PrimaryColor[] PrimaryColors { get; } = Enum.GetValues<PrimaryColor>();

	/// <summary>
	/// The sequence of the accent colors of the application.
	/// </summary>
	public static SecondaryColor[] SecondaryColors { get; } = Enum.GetValues<SecondaryColor>();

	/// <summary>
	/// Current settings for user to change.
	/// </summary>
	public AppSettings CurrentSettings { get; }

	/// <summary>
	/// Specifies that the <see cref="BaseThemeMode.Dark" /> theme is used.
	/// </summary>
	[ObservableProperty]
	public partial bool IsDarkTheme { get; set; }

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

	/// <inheritdoc cref="AppSettings.PrimaryColor" />
	[ObservableProperty]
	public partial PrimaryColor PrimaryColor { get; set; }

	/// <inheritdoc cref="AppSettings.SecondaryColor" />
	[ObservableProperty]
	public partial SecondaryColor SecondaryColor { get; set; }

	/// <inheritdoc cref="AppSettings.TrackClipboardHistory" />
	[ObservableProperty]
	public partial bool TrackClipboardHistory { get; set; }

	/// <inheritdoc cref="AppSettings.TrackHotkeys" />
	[ObservableProperty]
	public partial bool TrackHotkeys { get; set; }
	#endregion

	#region Partial
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

		_settingsManager.SetAppMaterialTheme(
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

		_settingsManager.SetAppMaterialTheme(
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

		_settingsManager.SetAppMaterialTheme(
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
	/// Called when <see cref="PrimaryColor" /> changes.
	/// </summary>
	partial void OnPrimaryColorChanged(PrimaryColor value)
	{
		CurrentSettings.PrimaryColor = value;

		SaveAndCloseCommand.NotifyCanExecuteChanged();

		_settingsManager.SetAppMaterialTheme(
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

		_settingsManager.SetAppMaterialTheme(
			CurrentSettings.Theme,
			PrimaryColor,
			value);
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
	/// Saves settings and closes the view.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanSaveAndClose))]
	public void SaveAndClose()
	{
		IsSaved = true;

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
	/// <inheritdoc cref="IAppSettingsManager" />
	private readonly IAppSettingsManager _settingsManager;
	#endregion

	#region Constructors
	public SettingsViewModel(IAppSettingsManager settingsManager)
	{
		_settingsManager = settingsManager;

		CurrentSettings = settingsManager.Settings.DeepCopy() ?? IAppSettingsManager.CreateDefaultSettings();

		TrackClipboardHistory = CurrentSettings.TrackClipboardHistory;

		TrackHotkeys = CurrentSettings.TrackHotkeys;

		SecondaryColor = CurrentSettings.SecondaryColor;

		PrimaryColor = CurrentSettings.PrimaryColor;

		Language = new(CurrentSettings.Language);

		IsLightTheme = CurrentSettings.Theme == BaseThemeMode.Light;

		IsInheritTheme = CurrentSettings.Theme == BaseThemeMode.Inherit;

		IsDarkTheme = CurrentSettings.Theme == BaseThemeMode.Dark;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="SaveAndCloseCommand" />.
	/// </summary>
	private bool CanSaveAndClose() => !Equals(CurrentSettings, _settingsManager.Settings);
	#endregion
}
