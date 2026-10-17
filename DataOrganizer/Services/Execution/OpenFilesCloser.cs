using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Execution;
using DataOrganizer.Messages.Editor;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Execution;

/// <inheritdoc cref="IOpenFilesCloser" />
public sealed class OpenFilesCloser : IOpenFilesCloser
{
	#region Data
	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="IExecutingFiles" />
	private readonly IExecutingFiles _executingFiles;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;
	#endregion

	#region Constructors
	public OpenFilesCloser(
		IDialogService dialogService,
		IExecutingFiles executingFiles,
		IMessenger messenger)
	{
		_dialogService = dialogService;

		_executingFiles = executingFiles;

		_messenger = messenger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<bool> TryCloseAsync(IReadOnlyCollection<FileDto> openedFiles, CancellationToken token = default)
	{
		if (openedFiles.Count == 0)
		{
			return true;
		}

		if (!await _dialogService
			.RequestCloseFilesAsync(token)
			.ConfigureAwait(true))
		{
			return false;
		}

		foreach (FileDto file in openedFiles.Where(static x => x.IsEditing))
		{
			_messenger.Send(new CloseEditingFileMessage(file));
		}

		foreach (FileDto file in openedFiles.Where(static x => x.IsExecuting))
		{
			_executingFiles.Close(file);
		}

		return true;
	}
	#endregion
}
