using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using Shared.Interfaces;

namespace DataOrganizer.Services.Settings;

public sealed class WindowSettingsStore : IWindowSettingsStore
{
	#region Data
	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IJsonSerializer" />
	private readonly IJsonSerializer _jsonSerializer;
	#endregion

	#region Constructors
	public WindowSettingsStore(
		IAppEnvironment appEnvironment,
		IFileSystem fileSystem,
		IJsonSerializer jsonSerializer)
	{
		_appEnvironment = appEnvironment;

		_fileSystem = fileSystem;

		_jsonSerializer = jsonSerializer;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public T? Read<T>() => _jsonSerializer.DeserializeFromFile<T>(GetFilePath<T>());

	/// <inheritdoc />
	public void Write<T>(T settings)
	{
		_fileSystem.SerializeToJsonFile(
			settings,
			GetFilePath<T>(),
			false);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the path of the file the settings of type <typeparamref name="T" /> are kept in.
	/// </summary>
	private string GetFilePath<T>() => _appEnvironment.GetSettingsFilePath(typeof(T).Name);
	#endregion
}
