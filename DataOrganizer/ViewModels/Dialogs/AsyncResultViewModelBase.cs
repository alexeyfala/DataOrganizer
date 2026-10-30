using CommunityToolkit.Mvvm.ComponentModel;
using Diagnostics.Interfaces;
using DialogContracts.Constants;
using DialogContracts.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels.Dialogs;

public abstract class AsyncResultViewModelBase<TResult> : ObservableObject
{
	#region Properties
	/// <summary>
	/// Identifier of the dialog host that shows the dialog.
	/// </summary>
	public string DialogHostIdentifier { get; set; } = DialogHostIdentifiers.Main;
	#endregion

	#region Data
	/// <inheritdoc cref="IDialogCloser" />
	private readonly IDialogCloser _dialogCloser;

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
		IDialogCloser dialogCloser,
		ITaskExceptionHandler exceptionHandler)
	{
		_dialogCloser = dialogCloser;

		_exceptionHandler = exceptionHandler;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Sets a result and closes the dialog in its host; the first result wins.
	/// </summary>
	public async Task SetResultAsync(TResult result)
	{
		// Another button pressed while the dialog fades out must not replace the first answer.
		if (_isResultSet)
		{
			return;
		}

		_isResultSet = true;

		await _dialogCloser
			.CloseAsync(DialogHostIdentifier)
			.ConfigureAwait(true);

		_source.TrySetResult(result);
	}

	/// <summary>
	/// Awaits the answer, falling back to <paramref name="defaultResult" /> when the dialog closes without one.
	/// </summary>
	/// <param name="dialogClosed">Completes when the dialog closes, as the task that shows it in its host does.</param>
	/// <param name="defaultResult">Answer of a dialog closed without one.</param>
	/// <param name="token">Stops waiting for the answer.</param>
	protected Task<TResult> GetResultAsync(
		Task dialogClosed,
		TResult defaultResult,
		in CancellationToken token = default)
	{
		// A dialog that failed to show has no host to fade out.
		IShownDialog? shown = dialogClosed.IsCompleted ? null : _dialogCloser.FindShownDialog(DialogHostIdentifier);

		// The failure of a dialog that could not be shown goes to the log, and the caller still gets the default answer.
		_exceptionHandler.Watch(dialogClosed);

		_exceptionHandler.Watch(SetDefaultResultOnCloseAsync(dialogClosed, shown, defaultResult));

		return _source.Task.WaitAsync(token);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Answers with <paramref name="defaultResult" /> once the dialog has closed and faded out without an answer.
	/// </summary>
	private async Task SetDefaultResultOnCloseAsync(
		Task dialogClosed,
		IShownDialog? shown,
		TResult defaultResult)
	{
		await dialogClosed.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);

		if (_isResultSet)
		{
			return;
		}

		// Closing decides the answer: a button pressed while the dialog fades out is ignored.
		_isResultSet = true;

		if (shown is not null)
		{
			await shown
				.WaitUntilHiddenAsync()
				.ConfigureAwait(true);
		}

		_source.TrySetResult(defaultResult);
	}
	#endregion
}
