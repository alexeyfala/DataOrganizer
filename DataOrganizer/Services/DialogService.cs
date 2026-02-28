using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using DialogHostAvalonia;
using Serilog;
using Shared.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class DialogService : IDialogService
{
	#region Data
	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;
	#endregion

	#region Constructors
	public DialogService(ILogger logger, IViewFactory viewFactory)
	{
		_logger = logger;

		_viewFactory = viewFactory;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<string?> RequestUserPasswordAsync(string label, CancellationToken token = default)
	{
		_logger.LogInformation("Show password box");

		PasswordBox view = _viewFactory.CreateUserControl<PasswordBox>();

		view
			.ViewModel
			.Label = label;

		_ = DialogHost.Show(view);

		if (!await view
			.ViewModel
			.GetResultAsync(token: token)
			.ConfigureAwait(false) || view.ViewModel.Password is null)
		{
			return null;
		}

		try
		{
			return view
				.ViewModel
				.Password;
		}
		finally
		{
			view
				.ViewModel
				.Password = null;
		}
	}
	#endregion
}
