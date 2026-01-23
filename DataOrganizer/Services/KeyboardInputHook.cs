using Avalonia.Threading;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
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
	/// <inheritdoc cref="IClipboardService" />
	private readonly IClipboardService _clipboardService;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDispatcher" />
	private readonly IDispatcher _dispatcher;

	/// <inheritdoc cref="IGlobalHook" />
	private readonly IGlobalHook _hook;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notificationService;

	/// <inheritdoc cref="SemaphoreSlim" />
	private readonly SemaphoreSlim _semaphore = new(1, 1);
	#endregion

	#region Constructors
	public KeyboardInputHook(
		IClipboardService clipboardService,
		IDbAccess dbAccess,
		IDispatcher dispatcher,
		IGlobalHook hook,
		ILogger logger,
		INotificationService notificationService)
	{
		_clipboardService = clipboardService;

		_dbAccess = dbAccess;

		_dispatcher = dispatcher;

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
		_logger.LogInformation("Dispose global keyboard input tracking hook");

		_hook.KeyReleased -= Hook_KeyReleased;

		Files.Clear();

		InputStack.Clear();

		_hook.Dispose();
	}

	/// <summary>
	/// Handles the <see cref="IGlobalHook.KeyReleased" /> event.
	/// </summary>
	public async Task HandleKeyReleasedAsync(EventMask rawMask, KeyCode code)
	{
		try
		{
			await _semaphore
				.WaitAsync()
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
					.GetFileContentsAsync(file.Id)
					.ConfigureAwait(false);

				if (result.IsDefault() || !result.IsValid)
				{
					_logger.LogError($@"{Strings.FailedToLoadFileContents} of file ""{file.Id}""");

					return;
				}

				string text = TextHelper
					.Utf8Encoding
					.GetString(result.Contents);

				if (string.IsNullOrEmpty(text))
				{
					_logger.LogInformation($@"{Strings.ThereIsNoContentFor} ""{file.Name}""");

					return;
				}

				if (_clipboardService.FindClipboard() is not { } clipboard)
				{
					return;
				}

				if (AppDomain
					.CurrentDomain
					.IsRunningFromNUnit())
				{
					await clipboard
						.SetTextAsync(text)
						.ConfigureAwait(false);
				}
				else
				{
					_dispatcher.Post(() => _ = clipboard.SetTextAsync(text));
				}

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
			_semaphore.Release();
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
	public void StopTracking()
	{
		_logger.LogInformation("Stop global keyboard input tracking");

		Files.Clear();

		InputStack.Clear();

		_hook.Stop();
	}
	#endregion

	#region Service
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

				Files.ClearAddRange(hierarchy.GetFilesRecursively(x => x.Hotkeys.Count > 0));
			}
			finally
			{
				_semaphore.Release();
			}

			await Task
				.Delay(TimeSpan.FromSeconds(10), token)
				.ConfigureAwait(false);
		}
	}
	#endregion
}
