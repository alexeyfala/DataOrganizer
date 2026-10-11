using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Notifications;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrushExtensions = DataOrganizer.Extensions.BrushExtensions;

namespace DataOrganizer.ViewModels;

public abstract class CopyContentViewModelBase : ObservableDisposableBase
{
	#region Data
	/// <inheritdoc cref="Application" />
	protected readonly Application _app;

	/// <inheritdoc cref="IClipboardTextWriter" />
	protected readonly IClipboardTextWriter _clipboardWriter;

	/// <inheritdoc cref="IDbAccess" />
	protected readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDialogService" />
	protected readonly IDialogService _dialogService;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	protected readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IFileTextReader" />
	protected readonly IFileTextReader _fileTextReader;

	/// <inheritdoc cref="ILogger" />
	protected readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	protected readonly IMessenger _messenger;

	/// <inheritdoc cref="INotificationService" />
	protected readonly INotificationService _notification;
	#endregion

	#region Constructors
	protected CopyContentViewModelBase(
		Application app,
		IClipboardTextWriter clipboardWriter,
		IDbAccess dbAccess,
		IDialogService dialogService,
		IFileTextReader fileTextReader,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		ITaskExceptionHandler exceptionHandler)
	{
		_app = app;

		_clipboardWriter = clipboardWriter;

		_dbAccess = dbAccess;

		_dialogService = dialogService;

		_exceptionHandler = exceptionHandler;

		_fileTextReader = fileTextReader;

		_logger = logger;

		_messenger = messenger;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Finds the last container.
	/// </summary>
	protected static ItemsControl? FindLastContainer<T>(ItemsControl container, T[] parents) where T : class
	{
		if (parents.IsEmpty() || container.ContainerFromItem(parents[0]) is not ItemsControl item)
		{
			return container;
		}

		if (FindLastContainer(item, [.. parents.Skip(1)]) is ItemsControl subContainer)
		{
			return subContainer;
		}

		return container;
	}

	/// <summary>
	/// Copies the contents of an object to the system clipboard.
	/// </summary>
	protected async Task CopyContentAsync(
		FileDto file,
		ItemsControl container,
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

			FileTextReading reading = await _fileTextReader
				.ReadAsync(file, Strings.CopyContent, token)
				.ConfigureAwait(true);

			switch (reading.Status)
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

			if (this is ViewModelBase viewModel)
			{
				viewModel.InsertIntoCopyHistory(file, updateView);
			}

			await _clipboardWriter
				.WriteTextAsync(reading.Text, isSensitive: file.EncryptionStatus != EncryptionStatus.None)
				.ConfigureAwait(true);

			FolderDto[] parents = [.. file.GetAllParents().Reverse()];

			if (FindLastContainer(container, parents)?.ContainerFromItem(file) is TemplatedControl item)
			{
				_exceptionHandler.Watch(BrushExtensions.ApplyHighlightAnimationAsync(() => item.Background as Brush, token));
			}
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
	#endregion
}
