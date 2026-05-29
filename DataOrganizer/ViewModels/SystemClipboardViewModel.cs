using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO;
using DataOrganizer.Interfaces;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
	public ObservableCollection<ClipboardHistoryEntry> Entries => _clipboardHistory.Entries;

	/// <inheritdoc cref="ClipboardHistoryEntry" />
	[ObservableProperty]
	public partial ClipboardHistoryEntry? SelectedEntry { get; set; }
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
	/// Starts native window move-drag from any area.
	/// </summary>
	[RelayCommand]
	private static void PointerPressed(PointerPressedEventArgs? e)
	{
		if (e?.Source is not Visual visual || !e.GetCurrentPoint(visual).Properties.IsLeftButtonPressed)
		{
			return;
		}

		visual
			.FindLogicalAncestorOfType<Window>()?
			.BeginMoveDrag(e);
	}

	/// <summary>
	/// Clears the history list. Disabled while it is empty.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanClear))]
	private void Clear()
	{
		_clipboardHistory.Entries.Clear();

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
			_logger.LogException(ex, isAssertDebug: false);
		}
	}

	/// <summary>
	/// Restores <paramref name="entry" /> back into the system clipboard.
	/// </summary>
	[RelayCommand]
	private Task RestoreEntry(ClipboardHistoryEntry? entry)
	{
		return entry is null
			? Task.CompletedTask
			: _clipboardHistory.RestoreAsync(entry);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="ClearCommand" />.
	/// </summary>
	private bool CanClear() => _clipboardHistory.Entries.Count > 0;
	#endregion
}
