using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Messages.Hotkeys;
using Repository.Dto;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.Linq;
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

	/// <inheritdoc cref="ICopyHistoryRecorder" />
	private readonly ICopyHistoryRecorder _copyHistory;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IFileTextCopier" />
	private readonly IFileTextCopier _fileTextCopier;

	/// <inheritdoc cref="IGlobalHookRunner" />
	private readonly IGlobalHookRunner _hookRunner;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMainWindowLauncher" />
	private readonly IMainWindowLauncher _mainWindowLauncher;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

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
		ICopyHistoryRecorder copyHistory,
		IDispatcherAccessor dispatcher,
		IFileTextCopier fileTextCopier,
		IGlobalHookRunner hookRunner,
		ILogger logger,
		IMainWindowLauncher mainWindowLauncher,
		IMessenger messenger,
		INotificationService notification,
		ISessionHierarchy sessionHierarchy,
		ITaskExceptionHandler exceptionHandler,
		TimeProvider timeProvider)
	{
		_copyHistory = copyHistory;

		_dispatcher = dispatcher;

		_exceptionHandler = exceptionHandler;

		_fileTextCopier = fileTextCopier;

		_hookRunner = hookRunner;

		_logger = logger;

		_mainWindowLauncher = mainWindowLauncher;

		_messenger = messenger;

		_notification = notification;

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
				.PostAsync(() => FindFileByHotkey(hierarchy, typed))
				.ConfigureAwait(false) is not { } file)
			{
				return;
			}

			// The keys of a hotkey that fired do not start the next one.
			InputStack.Clear();

			// The password of a locked keeper is asked for over the main window.
			if (file.EncryptionStatus == EncryptionStatus.Encrypted)
			{
				await ActivateWindowAsync().ConfigureAwait(false);
			}

			switch (await _fileTextCopier
				.CopyAsync(file, $"{Strings.CopyContent}: {file.Name}", token)
				.ConfigureAwait(false))
			{
				case FileTextStatus.Damaged:
					_logger.LogError($@"{Strings.FailedToLoadFileContents} of file ""{file.Id}""");

					return;

				case FileTextStatus.NotText:
					_logger.LogWarning($@"{Strings.NonTextFileContents} of file ""{file.Id}""");

					_notification.ShowToast($@"{Strings.NonTextFileContents}: ""{file.Name}""");

					return;

				case FileTextStatus.Empty:
					_logger.LogInformation($@"{Strings.ThereIsNoContentFor} ""{file.Name}""");

					return;

				case FileTextStatus.Closed:
					return;
			}

			_notification.ShowToast(string.Format(Strings.TheContentsCopiedToClipboard, file.Name));

			await _dispatcher
				.PostAsync(() => _copyHistory.Add(file, showAtOnce: true))
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

	#region Helpers
	/// <summary>
	/// Returns the file whose hotkey the typed keys end with, or <c>null</c> if there is none.
	/// </summary>
	private static FileDto? FindFileByHotkey(
		IEnumerable<ExplorerItemDtoBase> hierarchy,
		KeyStroke[] typed)
	{
		foreach (FileDto file in hierarchy.GetFilesBy(static x => x.Hotkeys.Count > 0))
		{
			KeyStroke[] hotkeys = ReadHotkey(file);

			if (hotkeys.SequenceEqual(typed.TakeLast(hotkeys.Length)))
			{
				return file;
			}
		}

		return null;
	}

	/// <summary>
	/// Returns the keys of the hotkey of a file with only the modifiers in their masks, as the typed keys have them.
	/// </summary>
	private static KeyStroke[] ReadHotkey(FileDto file) => [.. file
		.Hotkeys
		.ToKeyStrokes()
		.Select(static x => x with
		{
			Mask = x.Mask.ToModifiers()
		})];

	/// <summary>
	/// Activates the main window and opens its favorites.
	/// </summary>
	private Task ActivateWindowAsync() => _dispatcher.PostAsync(() =>
	{
		_mainWindowLauncher.ActivateMainWindow();

		_mainWindowLauncher.ShowFavorites();
	});
	#endregion
}
