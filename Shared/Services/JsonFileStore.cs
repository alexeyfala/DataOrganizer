using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.IO;
using System.IO.Abstractions;

namespace Shared.Services;

/// <inheritdoc cref="IJsonFileStore" />
public sealed class JsonFileStore : IJsonFileStore
{
	#region Data
	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IJsonSerializer" />
	private readonly IJsonSerializer _jsonSerializer;
	#endregion

	#region Constructors
	public JsonFileStore(IFileSystem fileSystem, IJsonSerializer jsonSerializer)
	{
		_fileSystem = fileSystem;

		_jsonSerializer = jsonSerializer;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public T? Read<T>(string filePath)
	{
		try
		{
			return _jsonSerializer.Deserialize<T>(_fileSystem.File.ReadAllText(filePath));
		}
		// Stryker disable once Block : equivalent: an emptied block returns the same default
		catch (Exception)
		{
			// A missing or malformed file leaves the caller with its defaults.
			return default;
		}
	}

	/// <inheritdoc />
	public void Write<T>(T value, string filePath)
	{
		// Windows refuses to overwrite a hidden file.
		if (_fileSystem.FileExists(filePath))
		{
			_fileSystem.SetFileHidden(filePath, false);
		}

		if (Path.GetDirectoryName(filePath) is { } parentDirectory)
		{
			_fileSystem
				.Directory
				.CreateDirectory(parentDirectory);
		}

		_fileSystem
			.File
			.WriteAllText(filePath, _jsonSerializer.Serialize(value, JsonDefaults.Options));
	}
	#endregion
}
