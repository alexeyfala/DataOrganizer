using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
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

	/// <inheritdoc cref="IEntityEcryption" />
	protected readonly IEntityEcryption _entityEcryption;

	/// <inheritdoc cref="ILogger" />
	protected readonly ILogger _logger;
	#endregion

	#region Constructors
	protected CopyContentViewModelBase(
		Application app,
		IDbAccess dbAccess,
		IEntityEcryption entityEcryption,
		ILogger logger)
	{
		_app = app;

		_dbAccess = dbAccess;

		_entityEcryption = entityEcryption;

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
		FileModelDto file,
		ItemsControl container,
		CancellationToken token = default)
	{
		try
		{
			if (TopLevel
				.GetTopLevel(container)?
				.Clipboard is not { } clipboard)
			{
				return;
			}

			ContentsIsValidPair result = await _dbAccess
				.GetFileContentsAsync(file.Id, token)
				.ConfigureAwait(true);

			if (!result.IsValid)
			{
				_logger.LogError($@"{Strings.FailedToLoadFileContents} of file ""{file.Id}""");

				return;
			}

			if (!TryToDecrypt(
				result.Contents,
				file,
				out byte[] contents))
			{
				return;
			}

			string text = TextHelper
				.Utf8Encoding
				.GetString(contents);

			ViewModelBase? viewModel = _app.FindDataContext<ViewModelBase>();

			if (string.IsNullOrEmpty(text))
			{
				viewModel?.ShowInfoSnackbar($@"{Strings.ThereIsNoContentFor} ""{file.Name}""");

				return;
			}

			viewModel?.UpdateCopyHistory(file.Id);

			await clipboard
				.SetTextAsync(text)
				.ConfigureAwait(true);

			FolderModelDto[] parents = [.. file.GetAllParents().Reverse()];

			if (FindLastContainer(container, parents)?.ContainerFromItem(file) is not TemplatedControl item)
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

	/// <summary>
	/// Tries to decrypt the content, if it is encrypted.
	/// </summary>
	protected bool TryToDecrypt(
		byte[] input,
		FileModelDto file,
		out byte[] output)
	{
		output = input;

		return file.EncryptionStatus != EncryptionStatus.Decrypted ||
			(file.FindParent(x => x.EncryptedPassword is not null)?.EncryptedPassword is { } encryptedPassword
			&& encryptedPassword.Length != 0
			&& _entityEcryption.Decrypt(input, encryptedPassword, out output));
	}
	#endregion
}
