using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
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

public abstract class CopyContentViewModelBase : ObservableObject
{
	#region Data
	/// <inheritdoc cref="Application" />
	protected readonly Application _app;

	/// <inheritdoc cref="IDbAccess" />
	protected readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="ILogger" />
	protected readonly ILogger _logger;
	#endregion

	#region Constructors
	protected CopyContentViewModelBase(
		Application app,
		IDbAccess dbAccess,
		ILogger logger)
	{
		_app = app;

		_dbAccess = dbAccess;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Finds the last container.
	/// </summary>
	protected static ItemsControl? FindLastContainer<T>(ItemsControl container, T[] parents) where T : class
	{
		if (parents.Length == 0 || container.ContainerFromItem(parents[0]) is not ItemsControl item)
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
		FileModelDto dto,
		ItemsControl container,
		CancellationToken token = default)
	{
		try
		{
			if (TopLevel.GetTopLevel(container)?.Clipboard is not { } clipboard)
			{
				return;
			}

			ContentsIsValidPair result = await _dbAccess
				.GetFileContentsAsync(dto.Id, token)
				.ConfigureAwait(true);

			if (result.IsDefault() || !result.IsValid)
			{
				_logger.LogError($@"{Strings.FailedToLoadFileContents} of file ""{dto.Id}""");

				return;
			}

			string text = TextHelper
				.Utf8Encoding
				.GetString(result.Contents);

			ViewModelBase? viewModel = _app.FindDataContext<ViewModelBase>();

			if (string.IsNullOrEmpty(text))
			{
				viewModel?.ShowInfoSnackbar($@"{Strings.ThereIsNoContentFor} ""{dto.Name}""");

				return;
			}

			viewModel?.UpdateCopyHistory(dto.Id);

			await clipboard
				.SetTextAsync(text)
				.ConfigureAwait(true);

			FolderModelDto[] parents = [.. dto.GetParents().Reverse()];

			if (FindLastContainer(container, parents)?.ContainerFromItem(dto) is not TemplatedControl item)
			{
				return;
			}

			await BrushExtensions
				.ApplyLimeGreenColorAnimation(() => item.Background as Brush, token)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}
	#endregion
}
