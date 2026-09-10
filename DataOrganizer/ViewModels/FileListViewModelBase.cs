using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Notifications;
using Material.Icons.Avalonia;
using Repository.Dto;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

public abstract partial class FileListViewModelBase : CopyContentViewModelBase
{
	#region Auto-Generated Commands
	/// <summary>
	/// <see cref="InputElement.PointerExited" /> event handler of control for preview object's content.
	/// </summary>
	[RelayCommand]
	private static void PreviewPointerExited(MaterialIcon? icon)
	{
		if (icon is null)
		{
			return;
		}

		ToolTip.SetTip(icon, null);
	}

	/// <inheritdoc cref="CopyContentViewModelBase.CopyContentAsync" />
	[RelayCommand(CanExecute = nameof(CanCopyContent))]
	private void CopyContent(IEnumerable<object>? multiBindings)
	{
		object[] values = [.. multiBindings.AsNotNull()];

		if (!GetFile(
			values,
			out FileDto? file))
		{
			return;
		}

		if (!GetContainer(
			values,
			out SelectingItemsControl? container))
		{
			return;
		}

		container.SelectedItem = file;

		_exceptionHandler.Watch(CopyContentAsync(
			file: file,
			container: container,
			updateView: false));

	}

	/// <summary>
	/// <see cref="InputElement.PointerEntered" /> event handler of control for preview object's content.
	/// </summary>
	[RelayCommand]
	private async Task PreviewPointerEntered(MaterialIcon? icon)
	{
		if (icon?.DataContext is not FileDto file)
		{
			return;
		}

		ValidatedContents result = await _dbAccess
			.GetFileContentsAsync(file.Id)
			.ConfigureAwait(false);

		if (!result.IsValid)
		{
			_logger.LogError($@"{Strings.FailedToLoadFileContents} of file ""{file.Id}""");

			return;
		}

		byte[] contents = result.Contents;

		if (file.EncryptionStatus == EncryptionStatus.Decrypted)
		{
			try
			{
				contents = _contentCipher.Decrypt(file, contents);
			}
			catch (Exception ex) when (ex is CryptographicException or InvalidOperationException)
			{
				// A preview is rendered on demand, so the failure only reaches the log.
				_logger.LogException(ex);

				return;
			}
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

			ToolTip.SetTip(icon, text.Truncate(200));

			ToolTip.SetIsOpen(icon, true);

			_logger.LogDebug($@"Display content prewiew for ""{file.Id}""");
		}
		finally
		{
			if (file.EncryptionStatus == EncryptionStatus.Decrypted)
			{
				contents.ZeroMemory();
			}
		}
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
		IClipboardAccessor clipboard,
		IContentCipher contentCipher,
		IDbAccess dbAccess,
		IDialogService dialogService,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		ITaskExceptionHandler exceptionHandler) : base(
			app,
			clipboard,
			contentCipher,
			dbAccess,
			dialogService,
			logger,
			messenger,
			notification,
			exceptionHandler)
	{
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="CopyContentCommand" />.
	/// </summary>
	private static bool CanCopyContent(IEnumerable<object>? multiBindings)
	{
		if (GetFile(
			multiBindings?.ToArray() ?? [],
			out FileDto? file))
		{
			return !file.IsOpened();
		}

		return true;
	}

	/// <summary>
	/// Tries to get reference to container from multi bindings.
	/// </summary>
	private static bool GetContainer(
		object[] values,
		[NotNullWhen(true)] out SelectingItemsControl? container)
	{
		container = null;

		if (values.Length < 2 || values[1] is not SelectingItemsControl control)
		{
			return false;
		}

		container = control;

		return true;
	}

	/// <summary>
	/// Tries to get reference to file from multi bindings.
	/// </summary>
	private static bool GetFile(
		object[] values,
		[NotNullWhen(true)] out FileDto? file)
	{
		file = null;

		if (values.Length < 2 || values[0] is not FileDto dto)
		{
			return false;
		}

		file = dto;

		return true;
	}
	#endregion
}
