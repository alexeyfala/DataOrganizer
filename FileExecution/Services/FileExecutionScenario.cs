using DialogContracts.Interfaces;
using FileContents.Interfaces;
using FileExecution.Dto;
using FileExecution.Helpers;
using FileExecution.Interfaces;
using Hierarchy.Dto;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Threading.Tasks;

namespace FileExecution.Services;

/// <inheritdoc cref="IFileExecutionScenario" />
public sealed class FileExecutionScenario : IFileExecutionScenario
{
	#region Data
	/// <inheritdoc cref="IConfirmationPrompt" />
	private readonly IConfirmationPrompt _confirmationPrompt;

	/// <inheritdoc cref="IExecutingFiles" />
	private readonly IExecutingFiles _executingFiles;

	/// <inheritdoc cref="IFileContentsOpener" />
	private readonly IFileContentsOpener _fileContentsOpener;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public FileExecutionScenario(
		IConfirmationPrompt confirmationPrompt,
		IExecutingFiles executingFiles,
		IFileContentsOpener fileContentsOpener,
		ILogger logger)
	{
		_confirmationPrompt = confirmationPrompt;

		_executingFiles = executingFiles;

		_fileContentsOpener = fileContentsOpener;

		_logger = logger;
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

		if (await _fileContentsOpener
			.OpenAsync(file.Id, keeperId, file.Name, file.Kind)
			.ConfigureAwait(true) is not { } contents)
		{
			return;
		}

		_logger.LogInformation(
			$"Contents of the {contents.Length}-byte file loaded from the database:{file.GetPropertyValues(
				true,
				nameof(FileDto.Id),
				nameof(FileDto.Name),
				nameof(FileDto.Kind),
				nameof(FileDto.UpdatedAt))}");

		ExecuteFileParameters parameters = new()
		{
			Contents = contents,
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
