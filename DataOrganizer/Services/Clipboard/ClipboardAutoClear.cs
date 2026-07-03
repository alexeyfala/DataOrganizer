using Avalonia.Input;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Clipboard;

public sealed class ClipboardAutoClear : IClipboardAutoClear, IDisposable
{
	#region Data
	/// <summary>
	/// How long a sensitive payload is allowed to remain on the clipboard before being cleared.
	/// </summary>
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15.0);

	/// <inheritdoc cref="IClipboardAccessor" />
	private readonly IClipboardAccessor _clipboard;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IClipboardGate" />
	private readonly IClipboardGate _gate;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;

	/// <summary>
	/// Cancellation source for the current countdown; replaced (and the previous one cancelled) on each arm.
	/// </summary>
	private CancellationTokenSource? _cts;
	#endregion

	#region Constructors
	public ClipboardAutoClear(
		IClipboardAccessor clipboard,
		IClipboardGate gate,
		ILogger logger,
		ITaskExceptionHandler exceptionHandler,
		TimeProvider timeProvider)
	{
		_clipboard = clipboard;

		_gate = gate;

		_logger = logger;

		_exceptionHandler = exceptionHandler;

		_timeProvider = timeProvider;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Arm()
	{
		CancellationTokenSource next = new();

		// Restart the window: cancel and drop the previous countdown, if any.
		CancellationTokenSource? previous = Interlocked.Exchange(ref _cts, next);

		Cancel(previous);

		_exceptionHandler.Watch(RunAsync(next.Token));
	}

	/// <inheritdoc />
	public void Dispose() => Cancel(Interlocked.Exchange(ref _cts, null));
	#endregion

	#region Helpers
	/// <summary>
	/// Cancels and disposes <paramref name="cts" />, tolerating an already-disposed source.
	/// </summary>
	private static void Cancel(CancellationTokenSource? cts)
	{
		if (cts is null)
		{
			return;
		}

		try
		{
			cts.Cancel();
		}
		catch (ObjectDisposedException)
		{
			// Already disposed by a concurrent arm — nothing to do.
		}

		cts.Dispose();
	}

	/// <summary>
	/// Clears the system clipboard if it still carries this application's sensitive content.
	/// </summary>
	private async Task ClearIfStillOwnedAsync()
	{
		await _gate
			.WaitAsync()
			.ConfigureAwait(false);

		try
		{
			IReadOnlyList<DataFormat> formats = await _clipboard
				.GetDataFormatsAsync()
				.ConfigureAwait(false);

			// Something else was copied since — leave the current content alone.
			if (!ClipboardSensitivityMarkerWriter.ContainsOwnershipMarker(formats))
			{
				return;
			}

			await _clipboard
				.ClearAsync()
				.ConfigureAwait(false);

			_logger.LogDebug("Auto-cleared sensitive content from the clipboard after the timeout.");
		}
		finally
		{
			_gate.Release();
		}
	}

	/// <summary>
	/// Awaits the timeout and clears the clipboard unless the countdown was restarted or cancelled.
	/// </summary>
	private async Task RunAsync(CancellationToken token)
	{
		try
		{
			await Task
				.Delay(Timeout, _timeProvider, token)
				.ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			return;
		}

		await ClearIfStillOwnedAsync().ConfigureAwait(false);
	}
	#endregion
}
