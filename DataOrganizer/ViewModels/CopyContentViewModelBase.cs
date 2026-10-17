using Avalonia;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Notifications;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

public abstract class CopyContentViewModelBase : ObservableDisposableBase
{
	#region Properties
	/// <summary>
	/// Objects to highlight where the view shows them.
	/// </summary>
	public IObservable<ExplorerItemDtoBase> HighlightSignal => _highlightSignal;
	#endregion

	#region Data
	/// <inheritdoc cref="Application" />
	protected readonly Application _app;

	/// <inheritdoc cref="IDbAccess" />
	protected readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDialogService" />
	protected readonly IDialogService _dialogService;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	protected readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IFileTextCopier" />
	protected readonly IFileTextCopier _fileTextCopier;

	/// <inheritdoc cref="ILogger" />
	protected readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	protected readonly IMessenger _messenger;

	/// <inheritdoc cref="INotificationService" />
	protected readonly INotificationService _notification;

	/// <summary>
	/// Objects to highlight where the view shows them.
	/// </summary>
	private readonly Subject<ExplorerItemDtoBase> _highlightSignal = new();
	#endregion

	#region Constructors
	protected CopyContentViewModelBase(
		Application app,
		IDbAccess dbAccess,
		IDialogService dialogService,
		IFileTextCopier fileTextCopier,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		ITaskExceptionHandler exceptionHandler)
	{
		_app = app;

		_dbAccess = dbAccess;

		_dialogService = dialogService;

		_exceptionHandler = exceptionHandler;

		_fileTextCopier = fileTextCopier;

		_logger = logger;

		_messenger = messenger;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Inserts a copied file at the top of the copy history; a view model without the history keeps nothing.
	/// </summary>
	public virtual void InsertIntoCopyHistory(FileDto file, bool updateView)
	{
	}

	/// <summary>
	/// Copies the contents of an object to the system clipboard.
	/// </summary>
	protected async Task CopyContentAsync(
		FileDto file,
		bool updateView,
		CancellationToken token = default)
	{
		try
		{
			if (!await _dbAccess
				.ExistsAsync(file.Id, token)
				.ConfigureAwait(true))
			{
				_notification.ShowErrorSnackbar($@"""{file.Name}"" {Strings.DoesNotExist}");

				return;
			}

			switch (await _fileTextCopier
				.CopyAsync(file, Strings.CopyContent, token)
				.ConfigureAwait(true))
			{
				case FileTextStatus.Damaged:
					_notification.ShowErrorSnackbar($@"{Strings.FailedToLoadFileContents} ""{file.Name}""");

					return;

				case FileTextStatus.NotText:
					_notification.ShowWarningSnackbar($@"{Strings.NonTextFileContents}: ""{file.Name}""");

					return;

				case FileTextStatus.Empty:
					_notification.ShowInformationSnackbar($@"{Strings.ThereIsNoContentFor} ""{file.Name}""");

					return;

				case FileTextStatus.Closed:
					return;
			}

			InsertIntoCopyHistory(file, updateView);

			Highlight(file);
		}
		catch (OperationCanceledException)
		{
			// The copy was given up on, which is normal flow.
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			_notification.ShowErrorSnackbar($@"{Strings.FailedToLoadFileContents} ""{file.Name}""");
		}
	}

	/// <summary>
	/// Asks the view to highlight an object where it shows it.
	/// </summary>
	protected void Highlight(ExplorerItemDtoBase dto) => _highlightSignal.OnNext(dto);
	#endregion
}
