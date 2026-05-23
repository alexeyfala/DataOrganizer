using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DialogHostAvalonia;
using Shared.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Abstract;

public abstract class AsyncResultViewModelBase<TResult> : ObservableObject
{
	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _handler;

	/// <inheritdoc cref="TaskCompletionSource" />
	private readonly TaskCompletionSource<TResult> _source = new();

	/// <summary>
	/// <c>True</c> when the result has been set by <see cref="SetResultAsync" />.
	/// </summary>
	private bool _isResultSet;
	#endregion

	#region Constructors
	protected AsyncResultViewModelBase(
		Application app,
		ITaskExceptionHandler handler)
	{
		_app = app;

		_handler = handler;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Sets a result and closes <see cref="DialogHost" />.
	/// </summary>
	public async Task SetResultAsync(TResult result, CancellationToken token = default)
	{
		_isResultSet = true;

		if (_app.IsDialogHostOpened())
		{
			DialogOverlayPopupHost? host = DialogHost
				.GetDialogSession(null)?
				.Host;

			DialogHost.Close(null);

			if (host is not null)
			{
				Func<bool> condition = () => !host.IsActuallyOpen;

				await condition
					.WaitAsync(300, 10, token)
					.ConfigureAwait(false);
			}
		}

		_source.SetResult(result);
	}

	/// <summary>
	/// Returns a result.
	/// </summary>
	protected Task<TResult> GetResultAsync(TResult defaultResult, in CancellationToken token = default)
	{
		if (_app.IsDialogHostOpened())
		{
			_handler.Watch(WaitDialogCloseAsync(defaultResult, token));
		}

		return _source.Task;
	}
	#endregion

	#region Service
	/// <summary>
	/// Waits for the dialog <see cref="DialogHost" /> to close.
	/// </summary>
	/// <remarks>
	/// Needed in case the user closes the dialog without using provided buttons.
	/// </remarks>
	private async Task WaitDialogCloseAsync(
		TResult defaultResult,
		CancellationToken token = default)
	{
		while (_app.IsDialogHostOpened())
		{
			await Task
				.Delay(500, token)
				.ConfigureAwait(true);
		}

		if (_isResultSet)
		{
			return;
		}

		_source.SetResult(defaultResult);
	}
	#endregion
}
