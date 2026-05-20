using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Messages;
using Material.Icons.Avalonia;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.Abstract;

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
			out FileModelDto? file))
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

		_handler.Watch(CopyContentAsync(
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
		if (icon?.DataContext is not FileModelDto file)
		{
			return;
		}

		await Task
			.Delay(AppUtils.TipDelay)
			.ConfigureAwait(true);

		if (!icon.IsPointerOver)
		{
			return;
		}

		ContentsIsValidPair result = await _dbAccess
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
			if (_entityEncryption.TryToDecrypt(file, contents) is not { } decrypted)
			{
				return;
			}

			contents = decrypted;
		}

		try
		{
			string text = TextHelper
				.Utf8Encoding
				.GetString(contents);

			if (string.IsNullOrEmpty(text))
			{
				_messenger.Send(new ShowSnackbarMessage(new(
					$@"{Strings.ThereIsNoContentFor} ""{file.Name}""",
					SnackbarMessageLevel.Information)));

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

		_handler.Watch(viewModel.ShowInEditorAsync(id, window));
	}
	#endregion

	#region Constructors
	protected FileListViewModelBase(
		Application app,
		IClipboardService clipboard,
		IDbAccess dbAccess,
		IDialogService dialogService,
		IEntityEncryption entityEncryption,
		ILogger logger,
		IMessenger messenger,
		ITaskExceptionHandler handler) : base(
			app,
			clipboard,
			dbAccess,
			dialogService,
			entityEncryption,
			logger,
			messenger,
			handler)
	{
	}
	#endregion

	#region Service
	/// <summary>
	/// Validates <see cref="CopyContentCommand" />.
	/// </summary>
	private static bool CanCopyContent(IEnumerable<object>? multiBindings)
	{
		if (GetFile(
			multiBindings?.ToArray() ?? [],
			out FileModelDto? file))
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
		[NotNullWhen(true)] out FileModelDto? file)
	{
		file = null;

		if (values.Length < 2 || values[0] is not FileModelDto dto)
		{
			return false;
		}

		file = dto;

		return true;
	}
	#endregion
}
