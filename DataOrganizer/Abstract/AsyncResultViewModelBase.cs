using CommunityToolkit.Mvvm.ComponentModel;
using DialogHostAvalonia;
using Shared.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Abstract;

public abstract class AsyncResultViewModelBase<TResult> : ObservableObject
{
	#region Data
	/// <inheritdoc cref="TaskCompletionSource" />
	private readonly TaskCompletionSource<TResult> _source = new();

	/// <summary>
	/// Returns <c>True</c> if result is set my method <see cref="SetResultAsync" />.
	/// </summary>
	private bool _isResultSet;
	#endregion

	#region Methods
	/// <summary>
	/// Returns a result.
	/// </summary>
	protected Task<TResult> GetResultAsync(
		TResult defaultResult,
		CancellationToken token = default)
	{
		if (!AppDomain.CurrentDomain.IsRunningFromNUnit() && DialogHost.IsDialogOpen(null))
		{
			_ = WaitDialogCloseAsync(defaultResult, token);
		}

		return _source.Task;
	}

	/// <summary>
	/// Sets a result and closes <see cref="DialogHost" />.
	/// </summary>
	protected async Task SetResultAsync(TResult result, CancellationToken token = default)
	{
		_isResultSet = true;

		if (!AppDomain.CurrentDomain.IsRunningFromNUnit() && DialogHost.IsDialogOpen(null))
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
		while (DialogHost.IsDialogOpen(null))
		{
			await Task
				.Delay(500, token)
				.ConfigureAwait(false);
		}

		if (_isResultSet)
		{
			return;
		}

		_source.SetResult(defaultResult);
	}
	#endregion
}
