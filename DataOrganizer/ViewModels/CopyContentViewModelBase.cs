using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Notifications;
using Repository.Dto;
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

	/// <inheritdoc cref="IClipboardAccessor" />
	protected readonly IClipboardAccessor _clipboard;

	/// <inheritdoc cref="IContentCipher" />
	protected readonly IContentCipher _contentCipher;

	/// <inheritdoc cref="IDbAccess" />
	protected readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDialogService" />
	protected readonly IDialogService _dialogService;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	protected readonly ITaskExceptionHandler _exceptionHandler;

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
		IClipboardAccessor clipboard,
		IContentCipher contentCipher,
		IDbAccess dbAccess,
		IDialogService dialogService,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		ITaskExceptionHandler exceptionHandler)
	{
		_app = app;

		_clipboard = clipboard;

		_contentCipher = contentCipher;

		_dbAccess = dbAccess;

		_dialogService = dialogService;

		_exceptionHandler = exceptionHandler;

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

			ValidatedContents result = await _dbAccess
				.GetFileContentsAsync(file.Id, token)
				.ConfigureAwait(true);

			if (!result.IsValid)
			{
				_notification.ShowErrorSnackbar($@"{Strings.FailedToLoadFileContents} ""{file.Name}""");

				return;
			}

			if (await _contentCipher
				.TryDecryptContentsAsync(file, result.Contents, Strings.CopyContent, token)
				.ConfigureAwait(true) is not { } contents)
			{
				return;
			}

			try
			{
				string text = TextDefaults
					.Encoding
					.GetString(contents);

				if (string.IsNullOrEmpty(text))
				{
					_notification.ShowInformationSnackbar($@"{Strings.ThereIsNoContentFor} ""{file.Name}""");

					return;
				}

				if (this is ViewModelBase viewModel)
				{
					viewModel.InsertIntoCopyHistory(file, updateView);
				}

				try
				{
					await (file.EncryptionStatus != EncryptionStatus.None
						? _clipboard.SetDataAsync(ClipboardSensitivityMarkerWriter.CreateSensitiveText(text))
						: _clipboard.SetTextAsync(text))
						.ConfigureAwait(true);
				}
				catch (Exception ex)
				{
					_logger.LogException(ex);
				}

				FolderDto[] parents = [.. file.GetAllParents().Reverse()];

				if (FindLastContainer(container, parents)?.ContainerFromItem(file) is TemplatedControl item)
				{
					_exceptionHandler.Watch(BrushExtensions.ApplyHighlightAnimationAsync(() => item.Background as Brush, token));
				}
			}
			finally
			{
				if (file.EncryptionStatus != EncryptionStatus.None)
				{
					contents.ZeroMemory();
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}
	#endregion
}
