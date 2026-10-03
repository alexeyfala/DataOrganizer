using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces.Diagnostics;
using DialogHostAvalonia;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels.Dialogs;

public abstract class AsyncResultViewModelBase<TResult> : ObservableObject
{
	#region Data
	/// <summary>
	/// Longest wait for the closing animation of the dialog.
	/// </summary>
	private static readonly TimeSpan HideTimeout = TimeSpan.FromSeconds(3.0);

	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="TaskCompletionSource" />
	private readonly TaskCompletionSource<TResult> _source = new();

	/// <summary>
	/// <c>True</c> once the answer is decided, by <see cref="SetResultAsync" /> or by the dialog closing without one.
	/// </summary>
	private bool _isResultSet;
	#endregion

	#region Constructors
	protected AsyncResultViewModelBase(
		Application app,
		ITaskExceptionHandler exceptionHandler)
	{
		_app = app;

		_exceptionHandler = exceptionHandler;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Sets a result and closes <see cref="DialogHost" />; the first result wins.
	/// </summary>
	public async Task SetResultAsync(TResult result)
	{
		// Another button pressed while the dialog fades out must not replace the first answer.
		if (_isResultSet)
		{
			return;
		}

		_isResultSet = true;

		if (_app.IsDialogHostOpened())
		{
			DialogOverlayPopupHost? host = DialogHost
				.GetDialogSession(null)?
				.Host;

			DialogHost.Close(null);

			if (host is not null)
			{
				await WaitUntilHiddenAsync(host).ConfigureAwait(true);
			}
		}

		_source.TrySetResult(result);
	}

	/// <summary>
	/// Awaits the answer, falling back to <paramref name="defaultResult" /> when the dialog closes without one.
	/// </summary>
	/// <param name="dialogClosed">Completes when the dialog closes, as the task of <see cref="DialogHost.Show(object)" /> does.</param>
	/// <param name="defaultResult">Answer of a dialog closed without one.</param>
	/// <param name="token">Stops waiting for the answer.</param>
	protected Task<TResult> GetResultAsync(
		Task dialogClosed,
		TResult defaultResult,
		in CancellationToken token = default)
	{
		// A dialog that failed to show has no host to fade out.
		DialogOverlayPopupHost? host = !dialogClosed.IsCompleted && _app.IsDialogHostOpened()
			? DialogHost.GetDialogSession(null)?.Host
			: null;

		// The failure of a dialog that could not be shown goes to the log, and the caller still gets the default answer.
		_exceptionHandler.Watch(dialogClosed);

		_exceptionHandler.Watch(SetDefaultResultOnCloseAsync(dialogClosed, host, defaultResult));

		return _source.Task.WaitAsync(token);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Completes once the closing animation has hidden <paramref name="host" />, or after <see cref="HideTimeout" />.
	/// </summary>
	private static async Task WaitUntilHiddenAsync(DialogOverlayPopupHost host)
	{
		TaskCompletionSource hidden = new();

		using IDisposable subscription = host
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

	/// <summary>
	/// Answers with <paramref name="defaultResult" /> once the dialog has closed and faded out without an answer.
	/// </summary>
	private async Task SetDefaultResultOnCloseAsync(
		Task dialogClosed,
		DialogOverlayPopupHost? host,
		TResult defaultResult)
	{
		await dialogClosed.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);

		if (_isResultSet)
		{
			return;
		}

		// Closing decides the answer: a button pressed while the dialog fades out is ignored.
		_isResultSet = true;

		if (host is not null)
		{
			await WaitUntilHiddenAsync(host).ConfigureAwait(true);
		}

		_source.TrySetResult(defaultResult);
	}
	#endregion
}
