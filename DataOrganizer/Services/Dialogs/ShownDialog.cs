using Avalonia;
using Avalonia.Threading;
using DataOrganizer.Interfaces.Dialogs;
using DialogHostAvalonia;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Dialogs;

/// <inheritdoc cref="IShownDialog" />
public sealed class ShownDialog : IShownDialog
{
	#region Data
	/// <summary>
	/// Longest wait for the closing animation of the dialog.
	/// </summary>
	private static readonly TimeSpan HideTimeout = TimeSpan.FromSeconds(3.0);

	/// <summary>
	/// Popup of the host that shows the dialog.
	/// </summary>
	private readonly DialogOverlayPopupHost _host;
	#endregion

	#region Constructors
	public ShownDialog(DialogOverlayPopupHost host) => _host = host;
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task WaitUntilHiddenAsync()
	{
		TaskCompletionSource hidden = new();

		using IDisposable subscription = _host
			.GetObservable(DialogOverlayPopupHost.IsActuallyOpenProperty)
			.Subscribe(isOpen =>
			{
				if (!isOpen)
				{
					hidden.TrySetResult();
				}
			});

		// The animation runs on the render clock, so a stalled one must not hold the answer back.
		using IDisposable timeout = DispatcherTimer.RunOnce(() => hidden.TrySetResult(), HideTimeout);

		await hidden.Task.ConfigureAwait(true);
	}
	#endregion
}
