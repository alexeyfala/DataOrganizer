using Avalonia.Threading;
using DataOrganizer.DTO;
using DataOrganizer.Enums;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using DialogHostAvalonia;
using Repository.DTO;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class DialogService : IDialogService
{
	#region Data
	/// <inheritdoc cref="IDispatcher" />
	private readonly IDispatcher _dispatcher;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;
	#endregion

	#region Constructors
	public DialogService(
		IDispatcher dispatcher,
		ILogger logger,
		IViewFactory viewFactory)
	{
		_dispatcher = dispatcher;

		_logger = logger;

		_viewFactory = viewFactory;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<EditingHotkeysResult> EditHotkeysAsync(IEnumerable<CodeMaskPair> initialHotkeys)
	{
		HotkeysEditorView view = _viewFactory.CreateUserControl<HotkeysEditorView>();

		view
			.ViewModel
			.Buffer
			.AddRange(initialHotkeys);

		await DialogHost
			.Show(view)
			.ConfigureAwait(false);

		try
		{
			return new()
			{
				IsSaved = view.ViewModel.IsSaved,
				NewHotkeys = [.. view.ViewModel.Buffer]
			};
		}
		finally
		{
			view
				.ViewModel
				.Dispose();
		}
	}

	/// <inheritdoc />
	public async Task<bool> RequestCloseFilesAsync(CancellationToken token = default)
	{
		YesNoCancelBox view = _viewFactory.CreateUserControl<YesNoCancelBox>();

		view
			.ViewModel
			.Text = $"{Strings.CloseFilesBeingEdited}?";

		_ = DialogHost.Show(view);

		YesNoCancelResult result = await view
			.ViewModel
			.GetResultAsync(YesNoCancelVariant.YesCancel, token)
			.ConfigureAwait(false);

		return result == YesNoCancelResult.Yes;
	}

	/// <inheritdoc />
	public async Task<StringKeyValuePair?> RequestKeyValueInputAsync(
		KeyValueInputParameters parameters,
		CancellationToken token = default)
	{
		KeyValueInputView view = _viewFactory.CreateUserControl<KeyValueInputView>();

		view
			.ViewModel
			.Initialize(parameters);

		_ = DialogHost.Show(view);

		if (!await view
			.ViewModel
			.GetResultAsync(token)
			.ConfigureAwait(false) || view.ViewModel.Key is not { } key)
		{
			return null;
		}

		return new()
		{
			Key = key,
			Value = view.ViewModel.Value
		};
	}

	/// <inheritdoc />
	public async Task<ValueIsValidPair> RequestMultilineTextAsync(string? text, CancellationToken token = default)
	{
		MultilineTextEditView view = _viewFactory.CreateUserControl<MultilineTextEditView>();

		view
			.ViewModel
			.Text = text;

		_ = DialogHost.Show(view);

		if (!await view
			.ViewModel
			.GetResultAsync(token)
			.ConfigureAwait(false))
		{
			return new();
		}

		return new()
		{
			IsValid = true,
			Value = view.ViewModel.Text
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
			PasswordBox view = _viewFactory.CreateUserControl<PasswordBox>();

			view.Header = header;

			view.Label = label ?? Strings.Password;

			_ = DialogHost.Show(view);

			if (!await view
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
		});

		return source.Task;
	}

	/// <inheritdoc />
	public async Task<bool> RequestYesCancelDialogAsync(string text, CancellationToken token = default)
	{
		YesNoCancelBox view = _viewFactory.CreateUserControl<YesNoCancelBox>();

		view
			.ViewModel
			.Text = text;

		_ = DialogHost.Show(view);

		YesNoCancelResult result = await view
			.ViewModel
			.GetResultAsync(YesNoCancelVariant.YesCancel, token)
			.ConfigureAwait(false);

		return result == YesNoCancelResult.Yes;
	}

	/// <inheritdoc />
	public async Task<bool> RequestYesNoDialogAsync(string text, CancellationToken token = default)
	{
		YesNoCancelBox view = _viewFactory.CreateUserControl<YesNoCancelBox>();

		view
			.ViewModel
			.Text = text;

		_ = DialogHost.Show(view);

		YesNoCancelResult result = await view
			.ViewModel
			.GetResultAsync(YesNoCancelVariant.YesNo, token)
			.ConfigureAwait(false);

		return result == YesNoCancelResult.Yes;
	}

	/// <inheritdoc />
	public Task<ImportListVariant> SelectImportVariantAsync(CancellationToken token = default)
	{
		ImportListSelectorView view = _viewFactory.CreateUserControl<ImportListSelectorView>();

		view
			.ViewModel
			.Header = Strings.ImportList;

		_ = DialogHost.Show(view);

		return view
			.ViewModel
			.GetResultAsync(token);
	}

	/// <inheritdoc />
	public void ShowProperties(IEnumerable<PropertyNameValuePair> properties)
	{
		PropertiesView view = _viewFactory.CreateUserControl<PropertiesView>();

		view
			.ViewModel
			.Properties
			.AddRange(properties);

		DialogHost.Show(view);
	}
	#endregion
}
