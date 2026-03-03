using Avalonia.Threading;
using DataOrganizer.Enums;
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
	public Task<string?> RequestUserPasswordAsync(
		string header,
		string? label = null,
		CancellationToken token = default)
	{
		_logger.LogInformation("Show password box");

		TaskCompletionSource<string?> source = new();

		_dispatcher.Post(async () =>
		{
			PasswordBox view = _viewFactory.CreateUserControl<PasswordBox>();

			view
				.ViewModel
				.Header = header;

			view
				.ViewModel
				.Label = label ?? Strings.Password;

			_ = DialogHost.Show(view);

			if (!await view
				.ViewModel
				.GetResultAsync(token)
				.ConfigureAwait(false) || string.IsNullOrWhiteSpace(view.ViewModel.Password))
			{
				source.SetResult(null);

				return;
			}

			try
			{
				source.SetResult(view.ViewModel.Password);
			}
			finally
			{
				view
					.ViewModel
					.Password = null;
			}
		});

		return source.Task;
	}
	#endregion
}
