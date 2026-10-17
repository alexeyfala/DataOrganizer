using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Hotkeys;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Messages.Hotkeys;
using Repository.Dto;
using Serilog;
using Shared.Extensions;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Hotkeys;

public sealed class KeyboardInputHook :
	IKeyboardInputHook,
	IRecipient<GlobalKeyReleasedMessage>
{
	#region Properties
	/// <inheritdoc />
	public bool IsRunning => _hookRunner.IsRunning;

	/// <summary>
	/// Objects whose files are searched for a typed hotkey; <c>null</c> while tracking is stopped.
	/// </summary>
	internal IEnumerable<ExplorerItemDtoBase>? Hierarchy { get; set; }

	/// <summary>
	/// Stack of pressed keys.
	/// </summary>
	internal List<KeyStroke> InputStack { get; } = [];
	#endregion

	#region Data
	/// <summary>
	/// Longest pause between the keys of one hotkey; a longer one starts a new hotkey.
	/// </summary>
	private static readonly TimeSpan StrokeTimeout = TimeSpan.FromSeconds(3);

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IGlobalHookRunner" />
	private readonly IGlobalHookRunner _hookRunner;

	/// <inheritdoc cref="IHotkeyContentCopier" />
	private readonly IHotkeyContentCopier _hotkeyContentCopier;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="SemaphoreSlim" />
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	/// <inheritdoc cref="ISessionHierarchy" />
	private readonly ISessionHierarchy _sessionHierarchy;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;

	/// <summary>
	/// <c>True</c> when the service has already been disposed.
	/// </summary>
	private bool _isDisposed;

	/// <summary>
	/// Moment of the last key put on the input stack, or <c>null</c> before the first one.
	/// </summary>
	private long? _lastStrokeTimestamp;
	#endregion

	#region Constructors
	public KeyboardInputHook(
		IDispatcherAccessor dispatcher,
		IGlobalHookRunner hookRunner,
		IHotkeyContentCopier hotkeyContentCopier,
		ILogger logger,
		IMessenger messenger,
		ISessionHierarchy sessionHierarchy,
		ITaskExceptionHandler exceptionHandler,
		TimeProvider timeProvider)
	{
		_dispatcher = dispatcher;

		_exceptionHandler = exceptionHandler;

		_hookRunner = hookRunner;

		_hotkeyContentCopier = hotkeyContentCopier;

		_logger = logger;

		_messenger = messenger;

		_sessionHierarchy = sessionHierarchy;

		_timeProvider = timeProvider;

		messenger.RegisterAll(this);
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

		_semaphore.Dispose();

		_messenger.UnregisterAll(this);

		Hierarchy = null;

		InputStack.Clear();
	}

	/// <inheritdoc />
	public void Receive(GlobalKeyReleasedMessage message)
	{
		_exceptionHandler.Watch(HandleKeyReleasedAsync(
			message.Mask,
			message.Code));
	}

	/// <inheritdoc />
	public async Task StartTrackingAsync(CancellationToken token = default)
	{
		Func<bool> condition = () => !IsRunning;

		if (!await condition
			.WaitAsync(100, 10, token)
			.ConfigureAwait(false))
		{
			return;
		}

		await _semaphore
			.WaitAsync(token)
			.ConfigureAwait(false);

		try
		{
			_logger.LogInformation("Start global keyboard input tracking");

			await _hookRunner
				.StartAsync(token)
				.ConfigureAwait(false);

			if (!IsRunning)
			{
				return;
			}

			Hierarchy = _sessionHierarchy.Hierarchy;
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
	public async Task StopTrackingAsync(CancellationToken token = default)
	{
		await _semaphore
			.WaitAsync(token)
			.ConfigureAwait(false);

		try
		{
			_logger.LogInformation("Stop global keyboard input tracking");

			Hierarchy = null;

			InputStack.Clear();

			await _hookRunner
				.StopAsync(token)
				.ConfigureAwait(false);
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

	/// <summary>
	/// Handles a released key.
	/// </summary>
	internal async Task HandleKeyReleasedAsync(
		EventMask rawMask,
		KeyCode code,
		CancellationToken token = default)
	{
		await _semaphore
			.WaitAsync(token)
			.ConfigureAwait(false);

		try
		{
			if (Hierarchy is not { } hierarchy)
			{
				return;
			}

			// A modifier or a lock key takes part in a hotkey only through the mask, so releasing one breaks nothing.
			if (code.IsModifierOrLock())
			{
				return;
			}

			EventMask mask = rawMask.ToModifiers();

			// A key typed without a modifier ends the hotkey being typed.
			if (mask.IsDefault())
			{
				InputStack.Clear();

				return;
			}

			long timestamp = _timeProvider.GetTimestamp();

			if (_lastStrokeTimestamp is { } lastTimestamp && _timeProvider.GetElapsedTime(lastTimestamp, timestamp) > StrokeTimeout)
			{
				InputStack.Clear();
			}

			_lastStrokeTimestamp = timestamp;

			if (InputStack.Count == IKeyboardInputHook.MaxHotkeys)
			{
				InputStack.RemoveAt(0);
			}

			InputStack.Add(new()
			{
				Code = code,
				Mask = mask
			});

			KeyStroke[] typed = [.. InputStack];

			// The tree is read on the UI thread that changes it, so it cannot change during the search.
			if (await _dispatcher
				.PostAsync(() => HotkeyMatcher.FindFile(hierarchy, typed))
				.ConfigureAwait(false) is not { } file)
			{
				return;
			}

			// The keys of a hotkey that fired do not start the next one.
			InputStack.Clear();

			await _hotkeyContentCopier
				.CopyAsync(file, token)
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
