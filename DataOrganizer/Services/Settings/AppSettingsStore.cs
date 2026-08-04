using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Settings;
using Shared.Extensions;
using Shared.Interfaces;

namespace DataOrganizer.Services.Settings;

public sealed class AppSettingsStore : IAppSettingsStore
{
	#region Properties
	/// <inheritdoc />
	public AppSettings Settings { get; }
	#endregion

	#region Data
	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;
	#endregion

	#region Constructors
	public AppSettingsStore(
		IAppEnvironment appEnvironment,
		IFileSystem fileSystem,
		IJsonSerializerWrapper jsonSerializer)
	{
		_appEnvironment = appEnvironment;

		_fileSystem = fileSystem;

		Settings = LoadFromFile(jsonSerializer);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Overwrite(AppSettings value) => value.CopyPropertiesTo(Settings);

	/// <inheritdoc />
	public void Save()
	{
		_fileSystem.SerializeToJsonFile(
			Settings,
			GetFilePath(),
			false);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the path of the settings file.
	/// </summary>
	private string GetFilePath() => _appEnvironment.GetSettingsFilePath(nameof(AppSettings));

	/// <summary>
	/// Loads <see cref="AppSettings" /> data from file.
	/// Falls back to the default settings when the file is missing or malformed.
	/// </summary>
	private AppSettings LoadFromFile(IJsonSerializerWrapper jsonSerializer)
	{
		return jsonSerializer.FromFile<AppSettings>(GetFilePath()) is { } settings && settings.IsNotDefault()
			? settings
			: IAppSettingsStore.CreateDefaultSettings();
	}
	#endregion
}
