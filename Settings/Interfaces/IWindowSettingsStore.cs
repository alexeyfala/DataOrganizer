namespace Settings.Interfaces;

/// <summary>
/// Reads and writes the settings files of windows, one file for each type of settings, named after it.
/// </summary>
public interface IWindowSettingsStore
{
	#region Methods
	/// <summary>
	/// Reads the settings of type <typeparamref name="T" />; <c>default</c> when the file is missing or cannot be read.
	/// </summary>
	T? Read<T>();

	/// <summary>
	/// Writes <paramref name="settings" /> over the file of their type.
	/// </summary>
	void Write<T>(T settings);
	#endregion
}
