using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Messages;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrushExtensions = DataOrganizer.Extensions.BrushExtensions;

namespace DataOrganizer.Abstract;

public abstract class CopyContentViewModelBase : ObservableDisposableBase
{
	#region Data
	/// <inheritdoc cref="Application" />
	protected readonly Application _app;

	/// <inheritdoc cref="IClipboardService" />
	protected readonly IClipboardService _clipboard;

	/// <inheritdoc cref="IDbAccess" />
	protected readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDialogService" />
	protected readonly IDialogService _dialogService;

	/// <inheritdoc cref="IEntityEncryption" />
	protected readonly IEntityEncryption _entityEncryption;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	protected readonly ITaskExceptionHandler _handler;

	/// <inheritdoc cref="ILogger" />
	protected readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	protected readonly IMessenger _messenger;

	/// <inheritdoc cref="IViewModelExecutionService" />
	protected readonly IViewModelExecutionService _viewModel;
	#endregion

	#region Constructors
	protected CopyContentViewModelBase(
		Application app,
		IClipboardService clipboard,
		IDbAccess dbAccess,
		IDialogService dialogService,
		IEntityEncryption entityEncryption,
		ILogger logger,
		IMessenger messenger,
		ITaskExceptionHandler handler,
		IViewModelExecutionService viewModel)
	{
		_app = app;

		_clipboard = clipboard;

		_dbAccess = dbAccess;

		_dialogService = dialogService;

		_entityEncryption = entityEncryption;

		_handler = handler;

		_logger = logger;

		_messenger = messenger;

		_viewModel = viewModel;
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
		FileModelDto file,
		ItemsControl container,
		bool updateView,
		CancellationToken token = default)
	{
		try
		{
			if (!await _dbAccess
				.IsExistsAsync(file.Id, token)
				.ConfigureAwait(true))
			{
				SendMessage($@"""{file.Name}"" {Strings.DoesNotExist}", SnackbarMessageLevel.Error);

				return;
			}

			ContentsIsValidPair result = await _dbAccess
				.GetFileContentsAsync(file.Id, token)
				.ConfigureAwait(true);

			if (!result.IsValid)
			{
				SendMessage($@"{Strings.FailedToLoadFileContents} ""{file.Name}""", SnackbarMessageLevel.Error);

				return;
			}

			if (await _entityEncryption
				.TryToDecryptContentsAsync(file, result.Contents, Strings.CopyContent, token)
				.ConfigureAwait(true) is not { } contents)
			{
				return;
			}

			try
			{
				string text = TextHelper
					.Utf8Encoding
					.GetString(contents);

				if (string.IsNullOrEmpty(text))
				{
					SendMessage($@"{Strings.ThereIsNoContentFor} ""{file.Name}""", SnackbarMessageLevel.Information);

					return;
				}

				_viewModel.ExecuteInBaseViewModel(x => x.InsertToCopyHistory(file, updateView));

				await _clipboard
					.SetTextAsync(text)
					.ConfigureAwait(true);

				FolderModelDto[] parents = [.. file.GetAllParents().Reverse()];

				if (FindLastContainer(container, parents)?.ContainerFromItem(file) is TemplatedControl item)
				{
					_handler.Watch(BrushExtensions.ApplyLimeGreenColorAnimation(() => item.Background as Brush, token));
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

		void SendMessage(string message, SnackbarMessageLevel level)
		{
			_messenger.Send(new ShowSnackbarMessage(new ShowSnackbarPayload(message, level)));
		}
	}
	#endregion
}
