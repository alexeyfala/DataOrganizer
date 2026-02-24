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
using DataOrganizer.Views;
using DialogHostAvalonia;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Linq;
using System.Security.Cryptography;
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

	/// <inheritdoc cref="IViewFactory" />
	protected readonly IViewFactory _viewFactory;

	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;
	#endregion

	#region Constructors
	protected CopyContentViewModelBase(
		Application app,
		IDbAccess dbAccess,
		IEncryptionService encryption,
		IEntityEcryption entityEcryption,
		ILogger logger,
		IViewFactory viewFactory)
	{
		_app = app;

		_dbAccess = dbAccess;

		_encryption = encryption;

		_entityEcryption = entityEcryption;

		_logger = logger;

		_viewFactory = viewFactory;
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

			byte[] contents = result.Contents;

			if (file.EncryptionStatus == EncryptionStatus.Encrypted)
			{
				PasswordBox view = _viewFactory.CreateUserControl<PasswordBox>();

				_ = DialogHost.Show(view);

				try
				{
					// TODO: waitDialogHostCloses: false ony for favorites window
					if (!await view
						.ViewModel
						.GetResultAsync(waitDialogHostCloses: false, token: token)
						.ConfigureAwait(true) || view.ViewModel.Password is null)
					{
						return;
					}

					if (file.FindParent(x => x.IsPasswordKeeper()) is not { } root
						|| root.PasswordHash is null
						|| root.EncryptedDek is null)
					{
						return;
					}

					if (!_encryption.EnhancedVerify(view.ViewModel.Password, root.PasswordHash))
					{
						_app
							.FindDataContext<ViewModelBase>()?
							.ShowErrorSnackbar(Strings.IncorrectPassword);

						return;
					}

					if (!_encryption.Decrypt(
						root.EncryptedDek,
						TextHelper.Utf8Encoding.GetBytes(view.ViewModel.Password),
						out byte[] decryptedDek))
					{
						_app
							.FindDataContext<ViewModelBase>()?
							.ShowErrorSnackbar(Strings.FailedToProcessContents);

						return;
					}

					try
					{
						if (!_encryption.Decrypt(
							contents,
							decryptedDek,
							out contents))
						{
							_app
								.FindDataContext<ViewModelBase>()?
								.ShowErrorSnackbar(Strings.FailedToProcessContents);

							return;
						}
					}
					finally
					{
						CryptographicOperations.ZeroMemory(decryptedDek);
					}
				}
				finally
				{
					view
						.ViewModel
						.Password = null;
				}
			}
			else if (file.EncryptionStatus == EncryptionStatus.Decrypted && !TryToDecrypt(
				contents,
				file,
				out contents))
			{
				return;
			}

			try
			{
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
			finally
			{
				if (file.EncryptionStatus != EncryptionStatus.None)
				{
					CryptographicOperations.ZeroMemory(contents);
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <summary>
	/// Tries to decrypt the content, if it is decrypted.
	/// </summary>
	protected bool TryToDecrypt(
		byte[] input,
		FileModelDto file,
		out byte[] output)
	{
		output = input;

		return file.FindParent(x => x.IsPasswordKeeper()) is { } root
			&& root.SessionEncryptedDek is not null
			&& _entityEcryption.DecryptSessionContents(input, root.SessionEncryptedDek, out output);
	}
	#endregion
}
