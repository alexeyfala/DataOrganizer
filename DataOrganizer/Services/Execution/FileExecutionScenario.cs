using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Execution;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Helpers.Execution;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Interfaces.Notifications;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Execution;

public sealed class FileExecutionScenario : IFileExecutionScenario
{
	#region Data
	/// <inheritdoc cref="IConfirmationPrompt" />
	private readonly IConfirmationPrompt _confirmationPrompt;

	/// <inheritdoc cref="IExecutingFiles" />
	private readonly IExecutingFiles _executingFiles;

	/// <inheritdoc cref="IFileContentsReader" />
	private readonly IFileContentsReader _fileContentsReader;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public FileExecutionScenario(
		IConfirmationPrompt confirmationPrompt,
		IExecutingFiles executingFiles,
		IFileContentsReader fileContentsReader,
		ILogger logger,
		INotificationService notification)
	{
		_confirmationPrompt = confirmationPrompt;

		_executingFiles = executingFiles;

		_fileContentsReader = fileContentsReader;

		_logger = logger;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<bool> ConfirmAsync(FileDto file)
	{
		if (_executingFiles.IsExecuting(file.Id))
		{
			_logger.LogWarning($"The file is already executing in the operating system:{file.GetPropertyValues(
				true,
				nameof(FileDto.Id),
				nameof(FileDto.Name),
				nameof(FileDto.Kind))}");

			return false;
		}

		if (ExecutableFileDetector.IsExecutable(file.Name))
		{
			string text =
				$"{string.Format(Strings.TheFileMayRunProgramOrScriptOnYourDevice, file.Name, Environment.NewLine)}" +
				Environment.NewLine +
				Environment.NewLine +
				$"{Strings.OpenTheExecutableFile}?";

			if (!await _confirmationPrompt
				.RequestYesCancelAsync(text)
				.ConfigureAwait(true))
			{
				return false;
			}
		}

		_logger.LogInformation($"The file needs to be executed in the operating system:{file.GetPropertyValues(
			true,
			nameof(FileDto.Id),
			nameof(FileDto.Name),
			nameof(FileDto.Kind))}");

		return true;
	}

	/// <inheritdoc />
	public async Task ExecuteAsync(FileDto file, bool isReadOnly)
	{
		// The key that opens the contents also encrypts the changes made to them while the file runs.
		Guid? keeperId = file.FindUnlockedKeeper()?.Id;

		FileContentsReading reading;

		try
		{
			reading = await _fileContentsReader
				.ReadUnlockedAsync(file.Id, keeperId)
				.ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			_notification.ShowErrorSnackbar($@"{Strings.FailedToLoadFileContents} ""{file.Name}""");

			return;
		}

		if (reading.Status == FileContentsStatus.Damaged)
		{
			string errorText = $@"{Strings.FailedToLoadFileContents} ""{file.Name}""";

			_notification.ShowErrorSnackbar(errorText);

			_logger.LogError($"{errorText}:{file.GetPropertyValues(
				true,
				nameof(FileDto.Id),
				nameof(FileDto.Kind))}");

			return;
		}

		if (reading.Status == FileContentsStatus.Closed)
		{
			_notification.ShowErrorSnackbar(Strings.FailedToProcessContents);

			return;
		}

		_logger.LogInformation(
			$"Contents of the {reading.Contents.Length}-byte file loaded from the database:{file.GetPropertyValues(
				true,
				nameof(FileDto.Id),
				nameof(FileDto.Name),
				nameof(FileDto.Kind),
				nameof(FileDto.UpdatedAt))}");

		ExecuteFileParameters parameters = new()
		{
			Contents = reading.Contents,
			File = file,
			IsReadOnly = isReadOnly,
			KeeperId = keeperId,
		};

		await _executingFiles
			.ExecuteAsync(parameters)
			.ConfigureAwait(true);
	}
	#endregion
}
