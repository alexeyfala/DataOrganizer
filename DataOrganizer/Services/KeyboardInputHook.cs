using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using DataOrganizer.Abstract;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using SharpHook;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class KeyboardInputHook : IKeyboardInputHook
{
	#region Properties
	/// <summary>
	/// List of files with hotkeys.
	/// </summary>
	public List<FileModelDto> Files { get; } = [];

	/// <summary>
	/// Stack of pressed keys.
	/// </summary>
	public List<CodeMaskPair> InputStack { get; } = [];

	/// <inheritdoc />
	public bool IsRunning => _hook.IsRunning;
	#endregion

	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IClipboardService" />
	private readonly IClipboardService _clipboard;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDispatcher" />
	private readonly IDispatcher _dispatcher;

	/// <inheritdoc cref="IEntityEcryption" />
	private readonly IEntityEcryption _entityEcryption;

	/// <inheritdoc cref="IGlobalHook" />
	private readonly IGlobalHook _hook;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notificationService;

	/// <inheritdoc cref="SemaphoreSlim" />
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	/// <summary>
	/// Returns <c>True</c> if the service was disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	public KeyboardInputHook(
		Application app,
		IClipboardService clipboardService,
		IDbAccess dbAccess,
		IDispatcher dispatcher,
		IEntityEcryption entityEcryption,
		IGlobalHook hook,
		ILogger logger,
		INotificationService notificationService)
	{
		_app = app;

		_clipboard = clipboardService;

		_dbAccess = dbAccess;

		_dispatcher = dispatcher;

		_entityEcryption = entityEcryption;

		_hook = hook;

		_logger = logger;

		_notificationService = notificationService;

		hook.KeyReleased += Hook_KeyReleased;
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="GlobalHookBase.KeyReleased" /> event handler.
	/// </summary>
	private void Hook_KeyReleased(object? sender, KeyboardHookEventArgs e)
	{
		_ = HandleKeyReleasedAsync(
			e.RawEvent.Mask,
			e.Data.KeyCode);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}

		_logger.LogInformation($"Disposing: {GetType().Name}");

		_isDisposed = true;

		_semaphore.Dispose();

		_hook.KeyReleased -= Hook_KeyReleased;

		_hook.Dispose();

		Files.Clear();

		InputStack.Clear();
	}

	/// <summary>
	/// Handles the <see cref="IGlobalHook.KeyReleased" /> event.
	/// </summary>
	public async Task HandleKeyReleasedAsync(
		EventMask rawMask,
		KeyCode code,
		CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			if (Files.Count == 0)
			{
				return;
			}

			EventMask mask = rawMask.RemoveFlag(EventMask.NumLock);

			if (mask.IsDefault())
			{
				return;
			}

			if (InputStack.Count == IKeyboardInputHook.MaxHotkeys)
			{
				InputStack.RemoveAt(0);
			}

			InputStack.Add(new()
			{
				Code = code,
				Mask = mask
			});

			foreach (FileModelDto file in Files)
			{
				CodeMaskPair[] hotkeys = [.. file.Hotkeys.ToCodeMaskPairs()];

				if (!hotkeys.SequenceEqual(InputStack.TakeLast(hotkeys.Length)))
				{
					continue;
				}

				ContentsIsValidPair result = await _dbAccess
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

				if (await _entityEcryption
					.TryToDecryptContentsAsync(file, result.Contents, $"{Strings.CopyContent}: {file.Name}", token)
					.ConfigureAwait(false) is not { } contents)
				{
					return;
				}

				string text = TextHelper
					.Utf8Encoding
					.GetString(contents);

				if (string.IsNullOrEmpty(text))
				{
					_logger.LogInformation($@"{Strings.ThereIsNoContentFor} ""{file.Name}""");

					return;
				}

				await _clipboard
					.SetTextAsync(text)
					.ConfigureAwait(false);

				_notificationService.ShowToast(string.Format(Strings.TheContentsCopiedToClipboard, file.Name));

				return;
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
		finally
		{
			if (!_isDisposed)
			{
				_semaphore.Release();
			}
		}
	}

	/// <inheritdoc />
	public async Task StartTrackingAsync(
		IEnumerable<ExplorerModelBaseDto> hierarchy,
		CancellationToken token = default)
	{
		Func<bool> condition = () => !IsRunning;

		if (!await condition
			.WaitAsync(100, 10, token)
			.ConfigureAwait(false))
		{
			return;
		}

		try
		{
			_logger.LogInformation("Start global keyboard input tracking");

			_ = _hook.RunAsync();

			condition = () => IsRunning;

			if (!await condition
				.WaitAsync(100, 10, token)
				.ConfigureAwait(false))
			{
				return;
			}

			_ = FilterFilesAsync(hierarchy, token);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <inheritdoc />
	public async Task StopTrackingAsync(CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			_logger.LogInformation("Stop global keyboard input tracking");

			Files.Clear();

			InputStack.Clear();

			_hook.Stop();
		}
		finally
		{
			if (!_isDisposed)
			{
				_semaphore.Release();
			}
		}
	}
	#endregion

	#region Service
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

	/// <summary>
	/// Filters a sequence by <see cref="FileModelDto" /> with an interval.
	/// </summary>
	private async Task FilterFilesAsync(
		IEnumerable<ExplorerModelBaseDto> hierarchy,
		CancellationToken token)
	{
		while (!token.IsCancellationRequested && IsRunning)
		{
			try
			{
				await _semaphore
					.WaitAsync(token)
					.ConfigureAwait(false);

				Files.ClearAddRange(hierarchy.GetFilesBy(x => x.Hotkeys.Count > 0));
			}
			finally
			{
				if (!_isDisposed)
				{
					_semaphore.Release();
				}
			}

			await Task
				.Delay(TimeSpan.FromSeconds(10), token)
				.ConfigureAwait(false);
		}
	}
	#endregion
}
