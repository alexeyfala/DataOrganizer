using DataOrganizer.Infrastructure.AppInstance.Interfaces;
using DataOrganizer.Infrastructure.Settings.Interfaces;

namespace DataOrganizer.Infrastructure.Settings.Services;

/// <inheritdoc cref="IWindowSettingsStore" />
public sealed class WindowSettingsStore : IWindowSettingsStore
{
	#region Data
	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IJsonFileStore" />
	private readonly IJsonFileStore _jsonFileStore;
	#endregion

	#region Constructors
	public WindowSettingsStore(IAppEnvironment appEnvironment, IJsonFileStore jsonFileStore)
	{
		_appEnvironment = appEnvironment;

		_jsonFileStore = jsonFileStore;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public T? Read<T>() => _jsonFileStore.Read<T>(GetFilePath<T>());

	/// <inheritdoc />
	public void Write<T>(T settings) => _jsonFileStore.Write(settings, GetFilePath<T>());
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the path of the file the settings of type <typeparamref name="T" /> are kept in.
	/// </summary>
	private string GetFilePath<T>() => _appEnvironment.GetSettingsFilePath(typeof(T).Name);
	#endregion
}
