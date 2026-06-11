using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.Interfaces.Clipboard;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>CustomClipboardWindow</c>.
/// </summary>
public sealed partial class CustomClipboardViewModel : ObservableObject
{
	#region Properties
	/// <summary>
	/// History entries to display (delegated to <see cref="IClipboardHistoryService" />).
	/// </summary>
	public ObservableCollection<ClipboardHistoryEntryBase> Entries => _clipboardHistory.Entries;

	/// <summary>
	/// Whether the window stays open on focus loss and after a restore.
	/// </summary>
	[ObservableProperty]
	public partial bool KeepOpen { get; set; }

	/// <inheritdoc cref="ClipboardHistoryEntryBase" />
	[ObservableProperty]
	public partial ClipboardHistoryEntryBase? SelectedEntry { get; set; }
	#endregion

	#region Data
	/// <inheritdoc cref="IClipboardHistoryService" />
	private readonly IClipboardHistoryService _clipboardHistory;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public CustomClipboardViewModel(IClipboardHistoryService clipboardHistory, ILogger logger)
	{
		_clipboardHistory = clipboardHistory;

		_logger = logger;
	}
	#endregion

	#region Commands
	/// <summary>
	/// Clears the history list. Disabled while it is empty.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanClear))]
	private async Task Clear()
	{
		await _clipboardHistory
			.ClearAsync()
			.ConfigureAwait(true);

		ClearCommand.NotifyCanExecuteChanged();
	}

	/// <summary>
	/// Opens <paramref name="url" /> in the OS-default browser via shell-execute.
	/// </summary>
	[RelayCommand]
	private void OpenUrl(string? url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return;
		}

		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true,
			});
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);
		}
	}

	/// <summary>
	/// Restores <paramref name="entry" /> back into the system clipboard.
	/// </summary>
	[RelayCommand]
	private Task RestoreEntry(ClipboardHistoryEntryBase? entry)
	{
		return entry is null
			? Task.CompletedTask
			: _clipboardHistory.RestoreAsync(entry);
	}

	/// <summary>
	/// Toggles the pinned state of <paramref name="entry" />.
	/// </summary>
	[RelayCommand]
	private void TogglePin(ClipboardHistoryEntryBase? entry)
	{
		if (entry is null)
		{
			return;
		}

		_clipboardHistory.TogglePin(entry);

		ClearCommand.NotifyCanExecuteChanged();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="ClearCommand" />.
	/// </summary>
	private bool CanClear() => _clipboardHistory.Entries.Any(static entry => !entry.IsPinned);
	#endregion
}
