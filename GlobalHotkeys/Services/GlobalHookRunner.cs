using CommunityToolkit.Mvvm.Messaging;
using Diagnostics.Interfaces;
using GlobalHotkeys.Interfaces;
using GlobalHotkeys.Messages;
using Serilog;
using Shared.Extensions;
using SharpHook;
using SharpHook.Data;
using System;
using System.Threading;
using System.Threading.Tasks;
using Threading.Extensions;
using Threading.Helpers;

namespace GlobalHotkeys.Services;

/// <inheritdoc cref="IGlobalHookRunner" />
public sealed class GlobalHookRunner : IGlobalHookRunner
{
	#region Properties
	/// <inheritdoc />
	public bool IsRunning => _hook.IsRunning;
	#endregion

	#region Data
	/// <summary>
	/// Time a start or a stop of the hook is waited for at most.
	/// </summary>
	private static readonly TimeSpan HookTimeout = TimeSpan.FromSeconds(1);

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IGlobalHook" />
	private readonly IGlobalHook _hook;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="SemaphoreSlim" />
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;

	/// <summary>
	/// <c>True</c> when the service has already been disposed.
	/// </summary>
	private bool _isDisposed;

	/// <summary>
	/// Task of the running hook; it ends once the hook has let go of the native hook.
	/// </summary>
	private Task _runTask = Task.CompletedTask;

	/// <summary>
	/// Completes once the running hook reports itself as enabled or its run ends; <c>null</c> before the first start.
	/// </summary>
	private TaskCompletionSource? _started;
	#endregion

	#region Constructors
	public GlobalHookRunner(
		IGlobalHook hook,
		ILogger logger,
		IMessenger messenger,
		ITaskExceptionHandler exceptionHandler,
		TimeProvider timeProvider)
	{
		_exceptionHandler = exceptionHandler;

		_hook = hook;

		_logger = logger;

		_messenger = messenger;

		_timeProvider = timeProvider;

		hook.HookEnabled += Hook_HookEnabled;

		hook.KeyReleased += Hook_KeyReleased;
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="GlobalHookBase.HookEnabled" /> event handler.
	/// </summary>
	private void Hook_HookEnabled(object? sender, HookEventArgs e) => _started?.TrySetResult();

	/// <summary>
	/// <see cref="GlobalHookBase.KeyReleased" /> event handler.
	/// </summary>
	private void Hook_KeyReleased(object? sender, KeyboardHookEventArgs e)
	{
		_messenger.Send(new GlobalKeyReleasedMessage(
			e.RawEvent.Mask,
			e.Data.KeyCode));
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

		_hook.HookEnabled -= Hook_HookEnabled;

		_hook.KeyReleased -= Hook_KeyReleased;

		_semaphore.Dispose();

		try
		{
			_hook.Dispose();
		}
		catch (HookException ex)
		{
			// The native hook may already be stopped — disposal must never throw.
			_logger.LogException(ex);
		}
	}

	/// <inheritdoc />
	public async Task StartAsync(CancellationToken token = default)
	{
		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		try
		{
			if (IsRunning)
			{
				return;
			}

			_logger.LogInformation("Start global keyboard hook");

			// The hook reports itself as enabled on its own thread, where the wait must not go on.
			TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);

			_started = started;

			// RunAsync ends its task before the hook lets go of the native hook; Run returns only after that.
			Task runTask = Task.Factory.StartNew(
				() => RunHook(started),
				CancellationToken.None,
				TaskCreationOptions.LongRunning,
				TaskScheduler.Default);

			_runTask = runTask;

			_exceptionHandler.Watch(runTask);

			// The native hook is installed asynchronously.
			bool isStarted = await WaitForAsync(
				started.Task,
				token).ConfigureAwait(false);

			if (isStarted)
			{
				return;
			}

			_logger.LogWarning("The global keyboard hook has not reported itself as enabled in time");
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <inheritdoc />
	public async Task StopAsync(CancellationToken token = default)
	{
		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		try
		{
			// Stopping a hook that has not finished starting fails.
			if (_started is { } started)
			{
				_ = await WaitForAsync(
					started.Task,
					token).ConfigureAwait(false);
			}

			if (!IsRunning)
			{
				return;
			}

			_logger.LogInformation("Stop global keyboard hook");

			_hook.Stop();

			// The native hook shuts down asynchronously.
			bool isStopped = await WaitForAsync(
				_runTask,
				token).ConfigureAwait(false);

			if (isStopped)
			{
				return;
			}

			_logger.LogWarning("The global keyboard hook has not stopped in time");
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Runs the hook on the calling thread until it stops and lets go of the native hook.
	/// </summary>
	private void RunHook(TaskCompletionSource started)
	{
		try
		{
			_hook.Run(GlobalHookType.Keyboard);
		}
		finally
		{
			// A hook that fails to start never reports itself as enabled.
			started.TrySetResult();
		}
	}

	/// <summary>
	/// Waits for <paramref name="signal" /> at most <see cref="HookTimeout" />; <c>false</c> when the time runs out.
	/// </summary>
	private async Task<bool> WaitForAsync(Task signal, CancellationToken token)
	{
		// A failed run is logged where it is watched, so only the end of the signal counts here.
		await signal
			.WaitAsync(
				HookTimeout,
				_timeProvider,
				token)
			.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

		token.ThrowIfCancellationRequested();

		return signal.IsCompleted;
	}
	#endregion
}
