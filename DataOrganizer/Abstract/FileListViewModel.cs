using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using Material.Icons.Avalonia;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataOrganizer.Abstract;

public abstract partial class FileListViewModel : CopyContentViewModelBase
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
	[RelayCommand]
	private void CopyContent(IEnumerable<object>? multiBindings)
	{
		object[] values = [.. multiBindings.AsNotNull()];

		if (values.Length < 2
			|| values[0] is not FileModelDto dto
			|| values[1] is not SelectingItemsControl container)
		{
			return;
		}

		container.SelectedItem = dto;

		_ = CopyContentAsync(dto, container);

	}

	/// <summary>
	/// <see cref="InputElement.PointerEntered" /> event handler of control for preview object's content.
	/// </summary>
	[RelayCommand]
	private async Task PreviewPointerEntered(MaterialIcon? icon)
	{
		if (icon?.DataContext is not FileModelDto dto)
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
			.GetFileContentsAsync(dto.Id)
			.ConfigureAwait(false);

		if (!result.IsValid)
		{
			_logger.LogError($@"{Strings.FailedToLoadFileContents} of file ""{dto.Id}""");

			return;
		}

		string text = TextHelper
			.Utf8Encoding
			.GetString(result.Contents);

		if (string.IsNullOrEmpty(text))
		{
			_app
				.FindDataContext<ViewModelBase>()?
				.ShowInfoSnackbar($@"{Strings.ThereIsNoContentFor} ""{dto.Name}""");

			return;
		}

		ToolTip.SetTip(icon, text.Truncate(200));

		ToolTip.SetIsOpen(icon, true);

		_logger.LogDebug($@"Display content prewiew for ""{dto.Id}""");
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

		_ = viewModel.ShowInEditorAsync(window, id);
	}
	#endregion

	#region Constructors
	protected FileListViewModel(
		Application app,
		IDbAccess dbAccess,
		IEntityEcryption entityEcryption,
		ILogger logger) : base(app, dbAccess, entityEcryption, logger)
	{
	}
	#endregion
}
