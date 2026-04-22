using Avalonia.Threading;
using DataOrganizer.Enums;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using DialogHostAvalonia;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
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
	public async Task<bool> RequestUserCloseFilesAsync(CancellationToken token = default)
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
	public Task<char[]> RequestUserPasswordAsync(
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
	#endregion
}
