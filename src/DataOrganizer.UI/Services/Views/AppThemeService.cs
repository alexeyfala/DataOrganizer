using Avalonia;
using Avalonia.Styling;
using DataOrganizer.Infrastructure.Settings.Dto;
using DataOrganizer.Infrastructure.Settings.Enums;
using DataOrganizer.Infrastructure.Settings.Interfaces;
using DataOrganizer.Shared.Extensions;
using DataOrganizer.UI.Extensions;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;
using Serilog;
using System;

namespace DataOrganizer.UI.Services.Views;

/// <inheritdoc cref="IAppThemeService" />
public sealed class AppThemeService : IAppThemeService
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IAppSettingsStore" />
	private readonly IAppSettingsStore _store;
	#endregion

	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="AppThemeService" /> class.
	/// </summary>
	public AppThemeService(
		Application app,
		IAppSettingsStore store,
		ILogger logger)
	{
		_app = app;

		_store = store;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void ApplyFromSettings()
	{
		AppSettings settings = _store.Settings;

		try
		{
			SetTheme(
				settings.Theme,
				settings.PrimaryColor,
				settings.SecondaryColor);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <inheritdoc />
	public void SetTheme(
		ThemeMode mode,
		ThemePrimaryColor primaryColor,
		ThemeSecondaryColor secondaryColor)
	{
		MaterialTheme appTheme = GetAppTheme();

		BaseThemeMode baseTheme = mode.ToMaterial();

		if (appTheme.BaseTheme != baseTheme)
		{
			appTheme.BaseTheme = baseTheme;

			_app.RequestedThemeVariant = mode switch
			{
				ThemeMode.Inherit => ThemeVariant.Default,
				ThemeMode.Light => ThemeVariant.Light,
				ThemeMode.Dark => ThemeVariant.Dark,
				_ => throw new NotImplementedException()
			};
		}

		appTheme.PrimaryColor = primaryColor.ToMaterial();

		appTheme.SecondaryColor = secondaryColor.ToMaterial();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the application theme.
	/// </summary>
	private MaterialTheme GetAppTheme() => _app.LocateMaterialTheme<MaterialTheme>();
	#endregion
}
