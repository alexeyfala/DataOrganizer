using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Interfaces;
using DataOrganizer.Messages;
using Serilog;
using Shared.Extensions;
using SharpHook;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class GlobalHookRunner : IGlobalHookRunner
{
	#region Properties
	/// <inheritdoc />
	public bool IsRunning => _hook.IsRunning;
	#endregion

	#region Data
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

	/// <summary>
	/// <c>True</c> when the service has already been disposed.
	/// </summary>
	private bool _isDisposed;

	/// <summary>
	/// <c>True</c> when the native hook has reported itself as enabled.
	/// </summary>
	private bool _isHookEnabled;

	/// <summary>
	/// Task of the running hook.
	/// </summary>
	private Task? _runTask;
	#endregion

	#region Constructors
	public GlobalHookRunner(
		IGlobalHook hook,
		ILogger logger,
		IMessenger messenger,
		ITaskExceptionHandler exceptionHandler)
	{
		_exceptionHandler = exceptionHandler;

		_hook = hook;

		_logger = logger;

		_messenger = messenger;

		hook.HookDisabled += Hook_HookDisabled;

		hook.HookEnabled += Hook_HookEnabled;

		hook.KeyReleased += Hook_KeyReleased;
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="GlobalHookBase.HookDisabled" /> event handler.
	/// </summary>
	private void Hook_HookDisabled(object? sender, HookEventArgs e) => _isHookEnabled = false;

	/// <summary>
	/// <see cref="GlobalHookBase.HookEnabled" /> event handler.
	/// </summary>
	private void Hook_HookEnabled(object? sender, HookEventArgs e) => _isHookEnabled = true;

	/// <summary>
	/// <see cref="GlobalHookBase.KeyReleased" /> event handler.
	/// </summary>
	//private void Hook_KeyReleased(object? sender, KeyboardHookEventArgs e) => KeyReleased?.Invoke(this, e);
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

		_hook.HookDisabled -= Hook_HookDisabled;

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
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			if (IsRunning)
			{
				return;
			}

			_logger.LogInformation("Start global keyboard hook");

			_isHookEnabled = false;

			Task runTask = _hook.RunAsync();

			_runTask = runTask;

			_exceptionHandler.Watch(runTask);

			// The native hook is installed asynchronously.
			Func<bool> condition = () => _isHookEnabled || runTask.IsCompleted;

			await condition
				.WaitAsync(100, 10, token)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task StopAsync(CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			// Stopping a hook that has not finished starting fails.
			Task? runTask = _runTask;

			Func<bool> condition = () => _isHookEnabled || runTask is null or { IsCompleted: true };

			await condition
				.WaitAsync(100, 10, token)
				.ConfigureAwait(false);

			if (!IsRunning)
			{
				return;
			}

			_logger.LogInformation("Stop global keyboard hook");

			_hook.Stop();

			// The native hook shuts down asynchronously.
			condition = () => !IsRunning;

			await condition
				.WaitAsync(100, 10, token)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}
	#endregion
}
