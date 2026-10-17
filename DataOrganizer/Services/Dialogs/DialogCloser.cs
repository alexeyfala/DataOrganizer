using Avalonia;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Dialogs;
using DialogHostAvalonia;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Dialogs;

/// <inheritdoc cref="IDialogCloser" />
public sealed class DialogCloser : IDialogCloser
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IUiTimer" />
	private readonly IUiTimer _uiTimer;
	#endregion

	#region Constructors
	public DialogCloser(Application app, IUiTimer uiTimer)
	{
		_app = app;

		_uiTimer = uiTimer;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task CloseAsync(string dialogHostIdentifier)
	{
		if (!_app.IsDialogHostOpened(dialogHostIdentifier))
		{
			return Task.CompletedTask;
		}

		IShownDialog? shown = FindShownDialog(dialogHostIdentifier);

		DialogHost.Close(dialogHostIdentifier);

		return shown?.WaitUntilHiddenAsync() ?? Task.CompletedTask;
	}

	/// <inheritdoc />
	public IShownDialog? FindShownDialog(string dialogHostIdentifier)
	{
		if (!_app.IsDialogHostOpened(dialogHostIdentifier)
			|| DialogHost.GetDialogSession(dialogHostIdentifier)?.Host is not { } host)
		{
			return null;
		}

		return new ShownDialog(host, _uiTimer);
	}
	#endregion
}
