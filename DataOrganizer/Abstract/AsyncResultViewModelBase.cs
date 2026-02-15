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
	#endregion

	#region Methods
	/// <summary>
	/// Returns a result.
	/// </summary>
	protected Task<TResult> GetResultAsync(
		TResult defaultResult,
		CancellationToken token = default)
	{
		if (!AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			_ = WaitDialogCloseAsync(defaultResult, token);
		}

		return _source.Task;
	}

	/// <summary>
	/// Sets a result and closes <see cref="DialogHost" />.
	/// </summary>
	protected void SetResult(TResult result)
	{
		if (!AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			DialogHost.Close(null);
		}

		_source.TrySetResult(result);
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

		_source.TrySetResult(defaultResult);
	}
	#endregion
}
