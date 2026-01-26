using Baksteen.Extensions.DeepCopy;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using DialogHostAvalonia;
using Material.Colors;
using Material.Styles.Themes.Base;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Globalization;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="SettingsView" />.
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
	/// Returns <c>True</c> if user has saved the settings.
	/// </summary>
	public bool IsSaved { get; private set; }
	#endregion

	#region Auto-Generated Properties
	/// <summary>
	/// Specifies that the <see cref="BaseThemeMode.Dark" /> theme is used.
	/// </summary>
	[ObservableProperty]
	private bool _isDarkTheme;

	/// <summary>
	/// Specifies that the <see cref="BaseThemeMode.Inherit" /> theme is used.
	/// </summary>
	[ObservableProperty]
	private bool _isInheritTheme;

	/// <summary>
	/// Specifies that the <see cref="BaseThemeMode.Light" /> theme is used.
	/// </summary>
	[ObservableProperty]
	private bool _isLightTheme;

	/// <summary>
	/// Returns <c>True</c> if an error should be displayed indicating that the file specified in <see cref="MasterPasswordFilePath" /> was not found.
	/// </summary>
	[ObservableProperty]
	private bool _isMasterPasswordFileNotFoundErrorVisible;

	/// <inheritdoc cref="AppSettings.Language" />
	[ObservableProperty]
	private CultureInfo? _language;

	/// <inheritdoc cref="AppSettings.MasterPasswordFilePath" />
	[ObservableProperty]
	private string? _masterPasswordFilePath;

	/// <inheritdoc cref="AppSettings.PrimaryColor" />
	[ObservableProperty]
	private PrimaryColor _primaryColor;

	/// <inheritdoc cref="AppSettings.SecondaryColor" />
	[ObservableProperty]
	private SecondaryColor _secondaryColor;

	/// <inheritdoc cref="AppSettings.TrackHotkeys" />
	[ObservableProperty]
	private bool _trackHotkeys;
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
	/// Called when <see cref="MasterPasswordFilePath" /> changes.
	/// </summary>
	partial void OnMasterPasswordFilePathChanged(string? value)
	{
		CurrentSettings.MasterPasswordFilePath = value;

		SaveAndCloseCommand.NotifyCanExecuteChanged();

		ValidateMasterPasswordFilePath(value);
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
	[RelayCommand(CanExecute = nameof(CanExecuteSaveAndClose))]
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
	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IAppSettingsManager" />
	private readonly IAppSettingsManager _settingsManager;
	#endregion

	#region Constructors
	public SettingsViewModel(IAppSettingsManager settingsManager, IFileSystem fileSystem)
	{
		_settingsManager = settingsManager;

		_fileSystem = fileSystem;

		CurrentSettings = settingsManager.Settings.DeepCopy() ?? IAppSettingsManager.CreateDefaultSettings();

		_isDarkTheme = CurrentSettings.Theme == BaseThemeMode.Dark;

		_isInheritTheme = CurrentSettings.Theme == BaseThemeMode.Inherit;

		_isLightTheme = CurrentSettings.Theme == BaseThemeMode.Light;

		_language = new(CurrentSettings.Language);

		_masterPasswordFilePath = CurrentSettings.MasterPasswordFilePath;

		_primaryColor = CurrentSettings.PrimaryColor;

		_secondaryColor = CurrentSettings.SecondaryColor;

		_trackHotkeys = CurrentSettings.TrackHotkeys;

		ValidateMasterPasswordFilePath(CurrentSettings.MasterPasswordFilePath);
	}
	#endregion

	#region Service
	/// <summary>
	/// Validates <see cref="SaveAndCloseCommand" />.
	/// </summary>
	private bool CanExecuteSaveAndClose() => !Equals(CurrentSettings, _settingsManager.Settings);

	/// <summary>
	/// Validates value in <see cref="MasterPasswordFilePath" />.
	/// </summary>
	private void ValidateMasterPasswordFilePath(string? path)
	{
		if (path is null)
		{
			IsMasterPasswordFileNotFoundErrorVisible = false;

			return;
		}

		if (!_fileSystem.IsFileExists(path))
		{
			IsMasterPasswordFileNotFoundErrorVisible = true;

			return;
		}
	}
	#endregion
}
