using DataOrganizer.Interfaces;
using DialogContracts.Interfaces;
using DialogHostAvalonia;
using System.Threading.Tasks;
using Threading.Interfaces;

namespace DataOrganizer.Services.Dialogs;

/// <inheritdoc cref="IDialogCloser" />
public sealed class DialogCloser : IDialogCloser
{
	#region Data
	/// <inheritdoc cref="IDialogHostFinder" />
	private readonly IDialogHostFinder _dialogHostFinder;

	/// <inheritdoc cref="IUiTimer" />
	private readonly IUiTimer _uiTimer;
	#endregion

	#region Constructors
	public DialogCloser(IDialogHostFinder dialogHostFinder, IUiTimer uiTimer)
	{
		_dialogHostFinder = dialogHostFinder;

		_uiTimer = uiTimer;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task CloseAsync(string dialogHostIdentifier)
	{
		if (!IsOpened(dialogHostIdentifier))
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
		if (!IsOpened(dialogHostIdentifier)
			|| DialogHost.GetDialogSession(dialogHostIdentifier)?.Host is not { } host)
		{
			return null;
		}

		return new ShownDialog(host, _uiTimer);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when the dialog host with <paramref name="dialogHostIdentifier" /> shows a dialog.
	/// </summary>
	private bool IsOpened(string dialogHostIdentifier)
	{
		return _dialogHostFinder.FindDialogHost(dialogHostIdentifier) is { IsOpen: true };
	}
	#endregion
}
