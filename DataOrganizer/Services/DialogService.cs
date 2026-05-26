using Avalonia.Threading;
using DataOrganizer.DTO;
using DataOrganizer.Enums;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using DialogHostAvalonia;
using Entities.Enums;
using Repository.DTO;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class DialogService : IDialogService
{
	#region Data
	/// <inheritdoc cref="IDispatcher" />
	private readonly IDispatcher _dispatcher;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _handler;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;
	#endregion

	#region Constructors
	public DialogService(
		IDispatcher dispatcher,
		ILogger logger,
		ITaskExceptionHandler handler,
		IViewFactory viewFactory)
	{
		_dispatcher = dispatcher;

		_handler = handler;

		_logger = logger;

		_viewFactory = viewFactory;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<EditingHotkeysResult> EditHotkeysAsync(IEnumerable<CodeMaskPair> initialHotkeys)
	{
		HotkeysEditorViewModel viewModel = _viewFactory.CreateViewModel<HotkeysEditorViewModel>();

		viewModel
			.Buffer
			.AddRange(initialHotkeys);

		await DialogHost
			.Show(_viewFactory.CreateUserControl<HotkeysEditorView>(viewModel))
			.ConfigureAwait(false);

		try
		{
			return new()
			{
				IsSaved = viewModel.IsSaved,
				NewHotkeys = [.. viewModel.Buffer]
			};
		}
		finally
		{
			viewModel.Dispose();
		}
	}

	/// <inheritdoc />
	public async Task<AssociatedAppInfo?> PickAppAsync(
		IEnumerable<AssociatedAppInfo> candidates,
		CancellationToken token = default)
	{
		AppPickerViewModel viewModel = _viewFactory.CreateViewModel<AppPickerViewModel>();

		viewModel.Header = Strings.OpenWith;

		viewModel
			.Candidates
			.AddRange(candidates);

		_handler.Watch(DialogHost.Show(_viewFactory.CreateUserControl<AppPickerView>(viewModel)));

		return await viewModel
			.GetResultAsync(token)
			.ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task<bool> RequestCloseFilesAsync(CancellationToken token = default)
	{
		YesNoCancelBoxViewModel viewModel = _viewFactory.CreateViewModel<YesNoCancelBoxViewModel>();

		viewModel.Text = $"{Strings.CloseFilesBeingEdited}?";

		_handler.Watch(DialogHost.Show(_viewFactory.CreateUserControl<YesNoCancelBox>(viewModel)));

		YesNoCancelResult result = await viewModel
			.GetResultAsync(YesNoCancelVariant.YesCancel, token)
			.ConfigureAwait(false);

		return result == YesNoCancelResult.Yes;
	}

	/// <inheritdoc />
	public async Task<StringKeyValuePair?> RequestKeyValueInputAsync(
		KeyValueInputParameters parameters,
		CancellationToken token = default)
	{
		KeyValueInputViewModel viewModel = _viewFactory.CreateViewModel<KeyValueInputViewModel>();

		viewModel.Initialize(parameters);

		_handler.Watch(DialogHost.Show(_viewFactory.CreateUserControl<KeyValueInputView>(viewModel)));

		if (!await viewModel
			.GetResultAsync(token)
			.ConfigureAwait(false) || viewModel.Key is not { } key)
		{
			return null;
		}

		return new()
		{
			Key = key,
			Value = viewModel.Value
		};
	}

	/// <inheritdoc />
	public async Task<ValueIsValidPair> RequestMultilineTextAsync(string? text, CancellationToken token = default)
	{
		MultilineTextEditViewModel viewModel = _viewFactory.CreateViewModel<MultilineTextEditViewModel>();

		viewModel.Text = text;

		_handler.Watch(DialogHost.Show(_viewFactory.CreateUserControl<MultilineTextEditView>(viewModel)));

		if (!await viewModel
			.GetResultAsync(token)
			.ConfigureAwait(false))
		{
			return new();
		}

		return new()
		{
			IsValid = true,
			Value = viewModel.Text
		};
	}

	/// <inheritdoc />
	public Task<char[]> RequestPasswordAsync(
		string header,
		string? label = null,
		CancellationToken token = default)
	{
		_logger.LogInformation("Show password box");

		TaskCompletionSource<char[]> source = new();

		_dispatcher.Post(async () =>
		{
			try
			{
				PasswordBoxViewModel viewModel = _viewFactory.CreateViewModel<PasswordBoxViewModel>();

				viewModel.Header = header;

				viewModel.Label = label ?? Strings.Password;

				PasswordBox view = _viewFactory.CreateUserControl<PasswordBox>(viewModel);

				_handler.Watch(DialogHost.Show(view));

				if (!await viewModel
					.GetResultAsync(token)
					.ConfigureAwait(true) || string.IsNullOrWhiteSpace(view.PasswordInput.Text))
				{
					source.SetResult([]);

					return;
				}

				try
				{
					using PinnedSecret secret = SecureStringHelper.CaptureAndWipe(view.
						PasswordInput
						.Text);

					source.SetResult(secret
						.AsReadOnlySpan()
						.ToArray());
				}
				finally
				{
					view
						.PasswordInput
						.Text = null;
				}
			}
			catch (Exception ex)
			{
				_logger.LogException(ex);

				source.SetException(ex);
			}
		});

		return source.Task;
	}

	/// <inheritdoc />
	public async Task<bool> RequestYesCancelDialogAsync(string text, CancellationToken token = default)
	{
		YesNoCancelBoxViewModel viewModel = _viewFactory.CreateViewModel<YesNoCancelBoxViewModel>();

		viewModel.Text = text;

		_handler.Watch(DialogHost.Show(_viewFactory.CreateUserControl<YesNoCancelBox>(viewModel)));

		YesNoCancelResult result = await viewModel
			.GetResultAsync(YesNoCancelVariant.YesCancel, token)
			.ConfigureAwait(false);

		return result == YesNoCancelResult.Yes;
	}

	/// <inheritdoc />
	public async Task<bool> RequestYesNoDialogAsync(string text, CancellationToken token = default)
	{
		YesNoCancelBoxViewModel viewModel = _viewFactory.CreateViewModel<YesNoCancelBoxViewModel>();

		viewModel.Text = text;

		_handler.Watch(DialogHost.Show(_viewFactory.CreateUserControl<YesNoCancelBox>(viewModel)));

		YesNoCancelResult result = await viewModel
			.GetResultAsync(YesNoCancelVariant.YesNo, token)
			.ConfigureAwait(false);

		return result == YesNoCancelResult.Yes;
	}

	/// <inheritdoc />
	public Task<ImportListVariant> SelectImportVariantAsync(CancellationToken token = default)
	{
		ImportListSelectorViewModel viewModel = _viewFactory.CreateViewModel<ImportListSelectorViewModel>();

		viewModel.Header = Strings.ImportList;

		_handler.Watch(DialogHost.Show(_viewFactory.CreateUserControl<ImportListSelectorView>(viewModel)));

		return viewModel.GetResultAsync(token);
	}

	/// <inheritdoc />
	public async Task<EntityCreationResult?> ShowEntityCreationAsync(CancellationToken token = default)
	{
		EntityCreationViewModel viewModel = _viewFactory.CreateViewModel<EntityCreationViewModel>();

		_handler.Watch(DialogHost.Show(_viewFactory.CreateUserControl<EntityCreationView>(viewModel)));

		try
		{
			if (!await viewModel
				.GetResultAsync(token)
				.ConfigureAwait(false))
			{
				return null;
			}

			EntityType entityType = viewModel switch
			{
				{ IsFolderSelected: true } => EntityType.Folder,
				{ IsFileSelected: true } => EntityType.File,
				{ IsDatasetSelected: true } => EntityType.DataSet,
				_ => throw new NotImplementedException()
			};

			return new()
			{
				Name = viewModel.Name,
				Type = entityType
			};
		}
		finally
		{
			viewModel.SaveSettingsInFile();
		}
	}

	/// <inheritdoc />
	public void ShowProperties(IEnumerable<PropertyNameValuePair> properties)
	{
		PropertiesViewModel viewModel = _viewFactory.CreateViewModel<PropertiesViewModel>();

		viewModel
			.Properties
			.AddRange(properties);

		DialogHost.Show(_viewFactory.CreateUserControl<PropertiesView>(viewModel));
	}

	/// <inheritdoc />
	public async Task<ShowSettingsResult> ShowSettingsAsync()
	{
		SettingsViewModel viewModel = _viewFactory.CreateViewModel<SettingsViewModel>();

		await DialogHost
			.Show(_viewFactory.CreateUserControl<SettingsView>(viewModel))
			.ConfigureAwait(false);

		return new()
		{
			IsSaved = viewModel.IsSaved,
			Settings = viewModel.CurrentSettings
		};
	}
	#endregion
}
