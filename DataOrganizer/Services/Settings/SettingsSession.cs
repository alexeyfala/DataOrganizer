using Baksteen.Extensions.DeepCopy;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Settings;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Settings;

/// <summary>
/// The one place of the settings session: the edited copy, the theme preview, the decision to confirm a close, and the
/// save or the rollback at its end.
/// </summary>
public sealed class SettingsSession : ISettingsCloseGuard, ISettingsCommit, ISettingsDraft
{
	#region Properties
	/// <inheritdoc />
	public bool IsDirty => !Equals(_draft, _settingsStore.Settings);

	/// <inheritdoc />
	public bool IsDiscarded { get; private set; }

	/// <inheritdoc />
	public bool IsSaved { get; private set; }

	/// <inheritdoc />
	public bool ShouldConfirmClose => !IsSaved && !IsDiscarded && IsDirty;
	#endregion

	#region Data
	/// <inheritdoc cref="IBackgroundServiceControl" />
	private readonly IBackgroundServiceControl _backgroundServiceControl;

	/// <inheritdoc cref="IAppSettingsStore" />
	private readonly IAppSettingsStore _settingsStore;

	/// <inheritdoc cref="IAppThemeService" />
	private readonly IAppThemeService _themeService;

	/// <summary>
	/// Copy of the saved settings that the session edits.
	/// </summary>
	private AppSettings? _draft;
	#endregion

	#region Constructors
	public SettingsSession(
		IAppSettingsStore settingsStore,
		IAppThemeService themeService,
		IBackgroundServiceControl backgroundServiceControl)
	{
		_backgroundServiceControl = backgroundServiceControl;

		_settingsStore = settingsStore;

		_themeService = themeService;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public AppSettings Begin()
	{
		_draft = _settingsStore.Settings.DeepCopy() ?? IAppSettingsStore.CreateDefaultSettings();

		IsDiscarded = false;

		IsSaved = false;

		return _draft;
	}

	/// <inheritdoc />
	public void Discard() => IsDiscarded = true;

	/// <inheritdoc />
	public async Task<AppSettings?> EndAsync(CancellationToken token = default)
	{
		if (!IsSaved || _draft is not { } settings)
		{
			_themeService.ApplyFromSettings();

			return null;
		}

		// Captured before the overwrite, so that only a change applies.
		bool wasPersistingClipboard = _settingsStore
			.Settings
			.PersistClipboardHistory;

		int previousAutoLockMinutes = _settingsStore
			.Settings
			.AutoLockMinutes;

		_settingsStore.Overwrite(settings);

		_settingsStore.Save();

		await _backgroundServiceControl
			.ApplySettingsAsync(
				settings,
				wasPersistingClipboard,
				previousAutoLockMinutes,
				token)
			.ConfigureAwait(false);

		return settings;
	}

	/// <inheritdoc />
	public void PreviewTheme(ThemeMode theme, ThemePrimaryColor primaryColor, ThemeSecondaryColor secondaryColor)
	{
		_themeService.SetTheme(
			theme,
			primaryColor,
			secondaryColor);
	}

	/// <inheritdoc />
	public void Save() => IsSaved = true;
	#endregion
}
