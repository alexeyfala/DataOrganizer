using Avalonia.Controls;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Execution;
using DataOrganizer.Enums.Dialogs;
using DataOrganizer.Enums.Exchange;
using DataOrganizer.Helpers.Notes;
using DataOrganizer.Helpers.Views;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.ViewModels.Dialogs;
using DataOrganizer.Views.Dialogs;
using DataOrganizer.Views.Settings;
using DataProtection.Enums;
using DataProtection.Helpers;
using DataProtection.Interfaces;
using DialogHostAvalonia;
using Entities.Enums;
using Repository.Dto;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Threading.Interfaces;

namespace DataOrganizer.Services.Dialogs;

public sealed class DialogService :
	IAppPickerPrompt,
	IConfirmationPrompt,
	IDialogService,
	IHotkeysPrompt,
	IImportModePrompt,
	IPasswordPrompt,
	ITextInputPrompt
{
	#region Data
	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="ISettingsCloseGuard" />
	private readonly ISettingsCloseGuard _settingsCloseGuard;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;
	#endregion

	#region Constructors
	public DialogService(
		IDispatcherAccessor dispatcher,
		ILogger logger,
		ISettingsCloseGuard settingsCloseGuard,
		IViewFactory viewFactory)
	{
		_dispatcher = dispatcher;

		_logger = logger;

		_settingsCloseGuard = settingsCloseGuard;

		_viewFactory = viewFactory;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<EditingHotkeysResult> EditHotkeysAsync(IEnumerable<KeyStroke> initialHotkeys)
	{
		HotkeysEditorViewModel viewModel = _viewFactory.CreateViewModel<HotkeysEditorViewModel>();

		viewModel
			.Buffer
			.AddRange(initialHotkeys);

		await DialogHost
			.Show(_viewFactory.CreateUserControl<HotkeysEditorView>(viewModel), DialogHostIdentifiers.Main)
			.ConfigureAwait(false);

		// The native hook is process-wide, it must be fully down before the next one starts.
		await viewModel
			.StopHookAsync()
			.ConfigureAwait(false);

		try
		{
			return new(viewModel.IsSaved, [.. viewModel.Buffer]);
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

		Task dialogClosed = ShowDialog<AppPickerView>(viewModel);

		return await viewModel
			.GetResultAsync(dialogClosed, token)
			.ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task<KeyValueInput?> RequestKeyValueInputAsync(
		KeyValueInputParameters parameters,
		CancellationToken token = default)
	{
		KeyValueInputViewModel viewModel = _viewFactory.CreateViewModel<KeyValueInputViewModel>();

		viewModel.Initialize(parameters);

		viewModel.DialogHostIdentifier = parameters.DialogHostIdentifier;

		Task dialogClosed = ShowDialog<KeyValueInputView>(viewModel, parameters.DialogHostIdentifier);

		if (!await viewModel
			.GetResultAsync(dialogClosed, token)
			.ConfigureAwait(false) || viewModel.Key is not { } key)
		{
			return null;
		}

		return new(key, viewModel.Value);
	}

	/// <inheritdoc />
	public async Task<TextInputResult> RequestMultilineTextAsync(
		string? text,
		string? name = null,
		bool isSensitive = false,
		CancellationToken token = default)
	{
		MultilineTextEditViewModel viewModel = _viewFactory.CreateViewModel<MultilineTextEditViewModel>();

		viewModel.Header = NoteHeaderBuilder.Build(name);

		viewModel.IsSensitive = isSensitive;

		viewModel.Text = text;

		Task dialogClosed = ShowDialog<MultilineTextEditView>(viewModel);

		if (!await viewModel
			.GetResultAsync(dialogClosed, token)
			.ConfigureAwait(false))
		{
			return new();
		}

		return new(IsConfirmed: true, Value: viewModel.Text);
	}

	/// <inheritdoc />
	public Task<PinnedSecret> RequestPasswordAsync(
		string header,
		string? label = null,
		string? description = null,
		PasswordPromptMode mode = PasswordPromptMode.Verify,
		CancellationToken token = default)
	{
		_logger.LogInformation("Show password box");

		TaskCompletionSource<PinnedSecret> source = new();

		_dispatcher.Post(async () =>
		{
			try
			{
				PasswordBoxViewModel viewModel = _viewFactory.CreateViewModel<PasswordBoxViewModel>();

				viewModel.Header = header;

				viewModel.Label = label ?? Strings.Password;

				viewModel.Description = description;

				viewModel.IsConfirmationVisible = mode == PasswordPromptMode.Create;

				PasswordBoxView view = _viewFactory.CreateUserControl<PasswordBoxView>(viewModel);

				Task dialogClosed = DialogHost.Show(
					view,
					DialogHostIdentifiers.Main);

				bool confirmed = await viewModel
					.GetResultAsync(dialogClosed, token)
					.ConfigureAwait(true);

				source.SetResult(CapturePasswordAndScrub(
					view.PasswordInput,
					confirmed,
					view.ConfirmationInput));
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
	public Task<bool> RequestYesCancelAsync(string text, CancellationToken token = default)
	{
		return RequestYesAsync(
			text,
			YesNoCancelButtons.YesCancel,
			DialogHostIdentifiers.Main,
			token);
	}

	/// <inheritdoc />
	public Task<bool> RequestYesNoAsync(
		string text,
		string dialogHostIdentifier = DialogHostIdentifiers.Main,
		CancellationToken token = default)
	{
		return RequestYesAsync(
			text,
			YesNoCancelButtons.YesNo,
			dialogHostIdentifier,
			token);
	}

	/// <inheritdoc />
	public Task<ImportMode> SelectImportModeAsync(CancellationToken token = default)
	{
		ImportListSelectorViewModel viewModel = _viewFactory.CreateViewModel<ImportListSelectorViewModel>();

		viewModel.Header = Strings.ImportList;

		Task dialogClosed = ShowDialog<ImportListSelectorView>(viewModel);

		return viewModel.GetResultAsync(dialogClosed, token);
	}

	/// <inheritdoc />
	public async Task<EntityCreationResult?> ShowEntityCreationAsync(CancellationToken token = default)
	{
		EntityCreationViewModel viewModel = _viewFactory.CreateViewModel<EntityCreationViewModel>();

		Task dialogClosed = ShowDialog<EntityCreationView>(viewModel);

		try
		{
			if (!await viewModel
				.GetResultAsync(dialogClosed, token)
				.ConfigureAwait(false))
			{
				return null;
			}

			EntityKind kind = viewModel switch
			{
				{ IsFolderSelected: true } => EntityKind.Folder,
				{ IsFileSelected: true } => EntityKind.File,
				{ IsDatasetSelected: true } => EntityKind.Dataset,
				_ => throw new NotImplementedException()
			};

			return new(viewModel.Name, kind);
		}
		finally
		{
			viewModel.SaveSettingsToFile();
		}
	}

	/// <inheritdoc />
	public void ShowProperties(IEnumerable<PropertyDescription> properties)
	{
		PropertiesViewModel viewModel = _viewFactory.CreateViewModel<PropertiesViewModel>();

		viewModel
			.Properties
			.AddRange(properties);

		ShowDialog<PropertiesView>(viewModel);
	}

	/// <inheritdoc />
	public async Task ShowSettingsAsync()
	{
		SettingsViewModel viewModel = _viewFactory.CreateViewModel<SettingsViewModel>();

		await DialogHost
			.Show(_viewFactory.CreateUserControl<SettingsView>(viewModel), DialogHostIdentifiers.Main, ClosingSettings)
			.ConfigureAwait(false);

		// Turns closing with unsaved changes (Escape, the close button, a click away) into
		// an in-place confirmation instead of a silent discard.
		void ClosingSettings(object sender, DialogClosingEventArgs args)
		{
			if (!_settingsCloseGuard.ShouldConfirmClose)
			{
				return;
			}

			args.Cancel();

			viewModel.IsConfirmingClose = true;
		}
	}

	/// <summary>
	/// Captures the password into a pinned buffer when confirmed with non-blank input,
	/// then scrubs and clears both source <see cref="TextBox" /> instances on every path.
	/// </summary>
	/// <returns>
	/// The captured secret, empty when not confirmed or blank; the caller owns it.
	/// </returns>
	internal static PinnedSecret CapturePasswordAndScrub(
		TextBox input,
		bool confirmed,
		TextBox? confirmation = null)
	{
		try
		{
			if (confirmed && !string.IsNullOrWhiteSpace(input.Text))
			{
				return StringWiper.CaptureAndWipe(input.Text);
			}

			return new(length: 0);
		}
		finally
		{
			Scrub(input);

			if (confirmation is not null)
			{
				Scrub(confirmation);
			}
		}

		static void Scrub(TextBox target)
		{
			if (!string.IsNullOrEmpty(target.Text))
			{
				StringWiper.Wipe(target.Text);
			}

			target.Text = null;
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Asks a question with the buttons in a dialog host; <c>True</c> when the answer is
	/// <see cref="YesNoCancelAnswer.Yes" />.
	/// </summary>
	private async Task<bool> RequestYesAsync(
		string text,
		YesNoCancelButtons buttons,
		string dialogHostIdentifier,
		CancellationToken token)
	{
		YesNoCancelBoxViewModel viewModel = _viewFactory.CreateViewModel<YesNoCancelBoxViewModel>();

		viewModel.Text = text;

		viewModel.DialogHostIdentifier = dialogHostIdentifier;

		Task dialogClosed = ShowDialog<YesNoCancelBoxView>(viewModel, dialogHostIdentifier);

		YesNoCancelAnswer result = await viewModel
			.GetResultAsync(dialogClosed, buttons, token)
			.ConfigureAwait(false);

		return result == YesNoCancelAnswer.Yes;
	}

	/// <summary>
	/// Shows the view of a view model in a dialog host; the task ends with the result of the dialog when it closes.
	/// </summary>
	private Task<object?> ShowDialog<TView>(object viewModel, string dialogHostIdentifier = DialogHostIdentifiers.Main)
		where TView : UserControl
	{
		return DialogHost.Show(
			_viewFactory.CreateUserControl<TView>(viewModel),
			dialogHostIdentifier);
	}
	#endregion
}
