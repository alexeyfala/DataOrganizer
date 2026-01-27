using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Views;
using DialogHostAvalonia;
using Humanizer;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="EncryptionView" />.
/// </summary>
public sealed partial class EncryptionViewModel : ObservableObject
{
	#region Auto-Generated Properties
	/// <summary>
	/// Error related to <see cref="MasterPasswordFilePath" />.
	/// </summary>
	[ObservableProperty]
	private string? _masterPasswordFileError;

	/// <summary>
	/// Information related to <see cref="MasterPasswordFilePath" />.
	/// </summary>
	[ObservableProperty]
	private string? _masterPasswordFileInfo;

	/// <inheritdoc cref="EncryptionSettings.MasterPasswordFilePath" />
	[ObservableProperty]
	private string? _masterPasswordFilePath;
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="MasterPasswordFilePath" /> changes.
	/// </summary>
	partial void OnMasterPasswordFilePathChanged(string? value) => _ = ValidateMasterPasswordFilePathAsync(value);
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Saves settings.
	/// </summary>
	[RelayCommand]
	private void Save()
	{
		SaveSettingsInFile();

		DialogHost.Close(null);
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IJsonSerializerWrapper" />
	private readonly IJsonSerializerWrapper _jsonSerializer;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public EncryptionViewModel(
		IFileSystem fileSystem,
		IJsonSerializerWrapper jsonSerializer,
		ILogger logger)
	{
		_fileSystem = fileSystem;

		_jsonSerializer = jsonSerializer;

		_logger = logger;

		EncryptionSettings settings = LoadSettingsFromFile();

		MasterPasswordFilePath = settings.MasterPasswordFilePath;
	}
	#endregion

	#region Service
	/// <summary>
	/// Loads <see cref="EncryptionSettings" /> data from file.
	/// </summary>
	private EncryptionSettings LoadSettingsFromFile()
	{
		string filePath = AppUtils.GetSettingsFilePath(nameof(EncryptionSettings));

		return _jsonSerializer.FromFile<EncryptionSettings>(filePath);
	}

	/// <summary>
	/// Saves <see cref="EncryptionSettings" /> in file.
	/// </summary>
	private void SaveSettingsInFile()
	{
		_fileSystem.SerializeToJsonFile(
			new EncryptionSettings
			{
				MasterPasswordFilePath = MasterPasswordFilePath,
			},
			AppUtils.GetSettingsFilePath(nameof(EncryptionSettings)),
			false);
	}

	/// <summary>
	/// Validates value in <see cref="MasterPasswordFilePath" />.
	/// </summary>
	private async Task ValidateMasterPasswordFilePathAsync(string? path)
	{
		MasterPasswordFileError = null;

		MasterPasswordFileInfo = null;

		if (path is null)
		{
			return;
		}

		if (!_fileSystem.IsFileExists(path))
		{
			MasterPasswordFileError = Strings.TheSpecifiedFileWasNotFound;

			return;
		}

		try
		{
			byte[] bytes = await _fileSystem
				.ReadAllBytesAsync(path)
				.ConfigureAwait(false);

			string size = bytes
				.Length
				.Bytes()
				.Humanize(CultureInfo.InvariantCulture);

			MasterPasswordFileInfo = $"{Strings.Size}: {size}";
		}
		catch (Exception ex)
		{
			MasterPasswordFileError = ex.Message;

			_logger.LogException(ex, isAssertDebug: false);
		}
	}
	#endregion
}
