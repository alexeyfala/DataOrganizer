using Avalonia;
using Avalonia.Styling;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Settings;
using Material.Colors;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;
using Serilog;
using Shared.Extensions;
using System;

namespace DataOrganizer.Services;

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
	public void ApplyMaterialTheme()
	{
		AppSettings settings = _store.Settings;

		try
		{
			SetAppMaterialTheme(
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
	public void SetAppMaterialTheme(
		BaseThemeMode mode,
		PrimaryColor primaryColor,
		SecondaryColor secondaryColor)
	{
		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		MaterialTheme appTheme = GetAppTheme();

		if (appTheme.BaseTheme != mode)
		{
			appTheme.BaseTheme = mode;

			_app.RequestedThemeVariant = mode switch
			{
				BaseThemeMode.Inherit => ThemeVariant.Default,
				BaseThemeMode.Light => ThemeVariant.Light,
				BaseThemeMode.Dark => ThemeVariant.Dark,
				_ => throw new NotImplementedException()
			};
		}

		appTheme.PrimaryColor = primaryColor;

		appTheme.SecondaryColor = secondaryColor;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the application theme.
	/// </summary>
	private MaterialTheme GetAppTheme() => _app.LocateMaterialTheme<MaterialTheme>();
	#endregion
}
