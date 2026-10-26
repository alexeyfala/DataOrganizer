using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Messages.Editor;
using Hierarchy.Dto;
using Shared.Properties;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Execution;

/// <inheritdoc cref="IOpenFilesCloser" />
public sealed class OpenFilesCloser : IOpenFilesCloser
{
	#region Data
	/// <inheritdoc cref="IConfirmationPrompt" />
	private readonly IConfirmationPrompt _confirmationPrompt;

	/// <inheritdoc cref="IExecutingFiles" />
	private readonly IExecutingFiles _executingFiles;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;
	#endregion

	#region Constructors
	public OpenFilesCloser(
		IConfirmationPrompt confirmationPrompt,
		IExecutingFiles executingFiles,
		IMessenger messenger)
	{
		_confirmationPrompt = confirmationPrompt;

		_executingFiles = executingFiles;

		_messenger = messenger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Close(IReadOnlyCollection<FileDto> openedFiles)
	{
		foreach (FileDto file in openedFiles.Where(static x => x.IsEditing))
		{
			_messenger.Send(new CloseEditingFileMessage(file));
		}

		foreach (FileDto file in openedFiles.Where(static x => x.IsExecuting))
		{
			_executingFiles.Close(file);
		}
	}

	/// <inheritdoc />
	public async Task<bool> TryCloseAsync(IReadOnlyCollection<FileDto> openedFiles, CancellationToken token = default)
	{
		if (openedFiles.Count == 0)
		{
			return true;
		}

		if (!await _confirmationPrompt
			.RequestYesCancelAsync($"{Strings.CloseFilesBeingEdited}?", token)
			.ConfigureAwait(true))
		{
			return false;
		}

		Close(openedFiles);

		return true;
	}
	#endregion
}
