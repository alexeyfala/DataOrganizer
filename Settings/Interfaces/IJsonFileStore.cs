namespace Settings.Interfaces;

/// <summary>
/// Keeps values in Json files.
/// </summary>
public interface IJsonFileStore
{
	#region Methods
	/// <summary>
	/// Reads the value kept in the file; the default value when the file is missing or cannot be read.
	/// </summary>
	T? Read<T>(string filePath);

	/// <summary>
	/// Writes the value into the file, creating its folder.
	/// </summary>
	void Write<T>(T value, string filePath);
	#endregion
}
