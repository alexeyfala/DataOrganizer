using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Messages.Hotkeys;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Windows;
using Repository.Dto;
using Repository.Interfaces.Database;
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

	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IClipboardAccessor" />
	private readonly IClipboardAccessor _clipboard;

	/// <inheritdoc cref="IContentCipher" />
	private readonly IContentCipher _contentCipher;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IGlobalHookRunner" />
	private readonly IGlobalHookRunner _hookRunner;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <inheritdoc cref="SemaphoreSlim" />
	private readonly SemaphoreSlim _semaphore = new(1, 1);

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
		Application app,
		IClipboardAccessor clipboardService,
		IContentCipher contentCipher,
		IDbAccess dbAccess,
		IDispatcherAccessor dispatcher,
		IGlobalHookRunner hookRunner,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		ITaskExceptionHandler exceptionHandler,
		TimeProvider timeProvider)
	{
		_app = app;

		_clipboard = clipboardService;

		_dbAccess = dbAccess;

		_dispatcher = dispatcher;

		_contentCipher = contentCipher;

		_exceptionHandler = exceptionHandler;

		_hookRunner = hookRunner;

		_logger = logger;

		_messenger = messenger;

		_notification = notification;

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
	public async Task StartTrackingAsync(
		IEnumerable<ExplorerItemDtoBase> hierarchy,
		CancellationToken token = default)
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

			Hierarchy = hierarchy;
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

			ValidatedContents result = await _dbAccess
				.GetFileContentsAsync(file.Id, token)
				.ConfigureAwait(false);

			if (!result.IsValid)
			{
				_logger.LogError($@"{Strings.FailedToLoadFileContents} of file ""{file.Id}""");

				return;
			}

			if (file.EncryptionStatus == EncryptionStatus.Encrypted)
			{
				await ActivateWindowAsync().ConfigureAwait(false);
			}

			if (await _contentCipher
				.TryDecryptContentsAsync(file, result.Contents, $"{Strings.CopyContent}: {file.Name}", token)
				.ConfigureAwait(false) is not { } contents)
			{
				return;
			}

			try
			{
				string text = TextDefaults
					.Encoding
					.GetString(contents);

				if (string.IsNullOrEmpty(text))
				{
					_logger.LogInformation($@"{Strings.ThereIsNoContentFor} ""{file.Name}""");

					return;
				}

				try
				{
					// Protected contents carry the markers that keep them out of the clipboard
					// history and hand them to the auto-clear, same as the copy command does.
					await (file.EncryptionStatus != EncryptionStatus.None
						? _clipboard.SetDataAsync(ClipboardSensitivityMarkerWriter.CreateSensitiveText(text))
						: _clipboard.SetTextAsync(text))
						.ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					_logger.LogException(ex);
				}

				_notification.ShowToast(string.Format(Strings.TheContentsCopiedToClipboard, file.Name));
			}
			finally
			{
				if (file.EncryptionStatus != EncryptionStatus.None)
				{
					contents.ZeroMemory();
				}
			}
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
	/// Activates the main window.
	/// </summary>
	private Task ActivateWindowAsync() => _dispatcher.PostAsync(() =>
	{
		if (_app.FindWindow<Window>(x => x.DataContext is ViewModelBase) is not { } window)
		{
			return;
		}

		if (window.WindowState == WindowState.Minimized)
		{
			window.WindowState = WindowState.Normal;
		}

		window.Activate();

		if (_app.FindDataContext<FavoritesViewModel>() is not { } faforites)
		{
			return;
		}

		faforites.ShowFavorites();
	});
	#endregion
}
