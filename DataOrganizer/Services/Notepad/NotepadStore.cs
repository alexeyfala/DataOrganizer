using DataOrganizer.Dto.Settings;
using DataOrganizer.Interfaces.Notepad;
using DataOrganizer.Interfaces.Runtime;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DataOrganizer.Services.Notepad;

/// <inheritdoc cref="INotepadStore" />
public sealed class NotepadStore : INotepadStore
{
	#region Data
	/// <summary>
	/// Extension of the files of the texts.
	/// </summary>
	private const string TextFileExtension = ".txt";

	/// <summary>
	/// Directory the files of the texts live in.
	/// </summary>
	private readonly string _directoryPath;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IJsonSerializer" />
	private readonly IJsonSerializer _jsonSerializer;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <summary>
	/// File the settings of the tabs live in.
	/// </summary>
	private readonly string _settingsFilePath;
	#endregion

	#region Constructors
	public NotepadStore(
		IAppEnvironment appEnvironment,
		IFileSystem fileSystem,
		IJsonSerializer jsonSerializer,
		ILogger logger)
	{
		_directoryPath = appEnvironment.NotepadDirectoryPath;

		_settingsFilePath = appEnvironment.GetSettingsFilePath(nameof(NotepadViewSettings));

		_fileSystem = fileSystem;

		_jsonSerializer = jsonSerializer;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Erase(int number)
	{
		string filePath = GetFilePath(number);

		if (!_fileSystem.FileExists(filePath))
		{
			return;
		}

		try
		{
			_fileSystem.EraseAndDeleteFile(filePath);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);
		}
	}

	/// <inheritdoc />
	public int[] FindNumbers()
	{
		if (!_fileSystem.DirectoryExists(_directoryPath))
		{
			return [];
		}

		try
		{
			return [.. _fileSystem
				.EnumerateFiles(_directoryPath)
				.Select(GetNumber)
				.OfType<int>()];
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			return [];
		}
	}

	/// <inheritdoc />
	public byte[]? Read(int number)
	{
		string filePath = GetFilePath(number);

		try
		{
			return _fileSystem.FileExists(filePath)
				? _fileSystem.ReadAllBytes(filePath)
				: [];
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			return null;
		}
	}

	/// <inheritdoc />
	public NotepadViewSettings? ReadSettings()
	{
		if (!_fileSystem.FileExists(_settingsFilePath))
		{
			return null;
		}

		try
		{
			return _jsonSerializer.Deserialize<NotepadViewSettings>(_fileSystem.ReadAllBytes(_settingsFilePath));
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			return null;
		}
	}

	/// <inheritdoc />
	public bool Write(int number, byte[] contents)
	{
		try
		{
			_fileSystem.CreateDirectory(_directoryPath);

			_fileSystem.WriteAllBytesAtomic(GetFilePath(number), contents);

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			return false;
		}
	}

	/// <inheritdoc />
	public void WriteSettings(NotepadViewSettings settings)
	{
		try
		{
			if (Path.GetDirectoryName(_settingsFilePath) is { } directoryPath)
			{
				_fileSystem.CreateDirectory(directoryPath);
			}

			_fileSystem.WriteAllBytesAtomic(
				_settingsFilePath,
				_jsonSerializer.SerializeToUtf8Bytes(settings, JsonDefaults.Options));
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the name of the file of the text of a tab.
	/// </summary>
	private static string GetFileName(int number) => number.ToString(CultureInfo.InvariantCulture) + TextFileExtension;

	/// <summary>
	/// Returns the number of a tab from the path of the file of its text; <c>null</c> for a file of another kind.
	/// </summary>
	private static int? GetNumber(string filePath)
	{
		// Only the names the store gives count, so no two files stand for one tab.
		if (!int.TryParse(
			Path.GetFileNameWithoutExtension(filePath),
			NumberStyles.None,
			CultureInfo.InvariantCulture,
			out int number)
			|| number < 1
			|| Path.GetFileName(filePath) != GetFileName(number))
		{
			return null;
		}

		return number;
	}

	/// <summary>
	/// Returns the path of the file of the text of a tab.
	/// </summary>
	private string GetFilePath(int number) => Path.Combine(_directoryPath, GetFileName(number));
	#endregion
}
