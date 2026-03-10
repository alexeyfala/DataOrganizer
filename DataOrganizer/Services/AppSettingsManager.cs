using Avalonia;
using Avalonia.Styling;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces;
using Material.Colors;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;

namespace DataOrganizer.Services;

public sealed class AppSettingsManager : IAppSettingsManager
{
	#region Properties
	/// <inheritdoc cref="AppSettings" />
	public AppSettings Settings { get; }
	#endregion

	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IJsonSerializerWrapper" />
	private readonly IJsonSerializerWrapper _jsonSerializer;

	/// <inheritdoc cref="ICommandLineOptions" />
	private readonly ICommandLineOptions _options;
	#endregion

	#region Constructors
	public AppSettingsManager(
		Application app,
		ICommandLineOptions options,
		IFileSystem fileSystem,
		IJsonSerializerWrapper jsonSerializer,
		ILogger logger)
	{
		_app = app;

		_fileSystem = fileSystem;

		_jsonSerializer = jsonSerializer;

		_options = options;

		Settings = LoadSettingsFromFile();

		try
		{
			ApplyMeterialTheme();

			Strings.Culture = new(Settings.Language);
		}
		catch (Exception ex)
		{
			logger.LogException(ex);

			Settings = IAppSettingsManager.CreateDefaultSettings();
		}
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void ApplyMeterialTheme()
	{
		SetAppMaterialTheme(
			Settings.Theme,
			Settings.PrimaryColor,
			Settings.SecondaryColor);
	}

	/// <inheritdoc />
	public void OverwriteSettings(AppSettings value) => value.CopyPropertiesTo(Settings);

	/// <inheritdoc />
	public void SaveSettingsInFile()
	{
		_fileSystem.SerializeToJsonFile(
			Settings,
			_options.GetSettingsFilePath(nameof(AppSettings)),
			false);
	}

	/// <inheritdoc />
	public void SetAppMaterialTheme(
		in BaseThemeMode mode,
		in PrimaryColor primaryColor,
		in SecondaryColor secondaryColor)
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

	#region Service
	/// <summary>
	/// Returns the application theme.
	/// </summary>
	private MaterialTheme GetAppTheme() => _app.LocateMaterialTheme<MaterialTheme>();

	/// <summary>
	/// Loads <see cref="AppSettings" /> data from file.
	/// </summary>
	private AppSettings LoadSettingsFromFile()
	{
		string filePath = _options.GetSettingsFilePath(nameof(AppSettings));

		return _jsonSerializer.FromFile<AppSettings>(filePath) is { } settings && settings.IsNotDefault()
			? settings
			: IAppSettingsManager.CreateDefaultSettings();
	}
	#endregion
}
