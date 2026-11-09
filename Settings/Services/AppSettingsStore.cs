using AppInstance.Interfaces;
using Settings.Dto;
using Settings.Interfaces;
using Shared.Extensions;

namespace Settings.Services;

/// <inheritdoc cref="IAppSettingsStore" />
public sealed class AppSettingsStore : IAppSettingsStore
{
	#region Properties
	/// <inheritdoc />
	public AppSettings Settings { get; }
	#endregion

	#region Data
	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IJsonFileStore" />
	private readonly IJsonFileStore _jsonFileStore;
	#endregion

	#region Constructors
	public AppSettingsStore(IAppEnvironment appEnvironment, IJsonFileStore jsonFileStore)
	{
		_appEnvironment = appEnvironment;

		_jsonFileStore = jsonFileStore;

		Settings = LoadFromFile();
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Overwrite(AppSettings value) => value.CopyPropertiesTo(Settings);

	/// <inheritdoc />
	public void Save() => _jsonFileStore.Write(Settings, GetFilePath());
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the path of the settings file.
	/// </summary>
	private string GetFilePath() => _appEnvironment.GetSettingsFilePath(nameof(AppSettings));

	/// <summary>
	/// Loads <see cref="AppSettings" /> data from a file.
	/// Falls back to the default settings when the file is missing or malformed.
	/// </summary>
	private AppSettings LoadFromFile()
	{
		return _jsonFileStore.Read<AppSettings>(GetFilePath()) is { } settings && settings.IsNotDefault()
			? settings
			: IAppSettingsStore.CreateDefaultSettings();
	}
	#endregion
}
