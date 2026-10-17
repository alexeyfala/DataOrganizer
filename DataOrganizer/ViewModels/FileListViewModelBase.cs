using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Notifications;
using Repository.Interfaces.Database;
using Serilog;
using System;

namespace DataOrganizer.ViewModels;

public abstract partial class FileListViewModelBase : CopyContentViewModelBase
{
	#region Auto-Generated Commands
	/// <inheritdoc cref="CopyContentViewModelBase.CopyContentAsync" />
	[RelayCommand(CanExecute = nameof(CanCopyContent))]
	private void CopyContent(FileDto? file)
	{
		if (file is null)
		{
			return;
		}

		SelectFile(file);

		_exceptionHandler.Watch(CopyContentAsync(
			file: file,
			updateView: false));
	}

	/// <summary>
	/// Displays object in "Editor".
	/// </summary>
	[RelayCommand]
	private void ShowInEditor(Guid id)
	{
		if (_app.FindDataContext<ViewModelBase>(out Window? window) is not { } viewModel || window is null)
		{
			return;
		}

		_exceptionHandler.Watch(viewModel.ShowInEditorAsync(id, window));
	}
	#endregion

	#region Constructors
	protected FileListViewModelBase(
		Application app,
		IDbAccess dbAccess,
		IDialogService dialogService,
		IFileTextCopier fileTextCopier,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		ITaskExceptionHandler exceptionHandler) : base(
			app,
			dbAccess,
			dialogService,
			fileTextCopier,
			logger,
			messenger,
			notification,
			exceptionHandler)
	{
	}
	#endregion

	#region Methods
	/// <summary>
	/// Selects a file of the list.
	/// </summary>
	protected abstract void SelectFile(FileDto file);
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="CopyContentCommand" />.
	/// </summary>
	private static bool CanCopyContent(FileDto? file) => file?.IsOpened() != true;
	#endregion
}
