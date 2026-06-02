using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO.Clipboard;
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
	public ObservableCollection<ClipboardHistoryEntryBase> Entries => _clipboardHistory.Entries;

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
	/// Starts native window move-drag from any area.
	/// </summary>
	[RelayCommand]
	private static void PointerPressed(PointerPressedEventArgs? e)
	{
		if (e?.Source is not Visual visual || !e.GetCurrentPoint(visual)
			.Properties
			.IsLeftButtonPressed)
		{
			return;
		}

		visual
			.FindLogicalAncestorOfType<Window>()?
			.BeginMoveDrag(e);
	}

	/// <summary>
	/// Starts native window resize-drag from the bottom edge.
	/// </summary>
	[RelayCommand]
	private static void ResizeByBottom(PointerPressedEventArgs? e) => BeginResize(e, WindowEdge.South);

	/// <summary>
	/// Starts native window resize-drag from the bottom-left corner.
	/// </summary>
	[RelayCommand]
	private static void ResizeByBottomLeft(PointerPressedEventArgs? e) => BeginResize(e, WindowEdge.SouthWest);

	/// <summary>
	/// Starts native window resize-drag from the bottom-right corner.
	/// </summary>
	[RelayCommand]
	private static void ResizeByBottomRight(PointerPressedEventArgs? e) => BeginResize(e, WindowEdge.SouthEast);

	/// <summary>
	/// Starts native window resize-drag from the left edge.
	/// </summary>
	[RelayCommand]
	private static void ResizeByLeft(PointerPressedEventArgs? e) => BeginResize(e, WindowEdge.West);

	/// <summary>
	/// Starts native window resize-drag from the right edge.
	/// </summary>
	[RelayCommand]
	private static void ResizeByRight(PointerPressedEventArgs? e) => BeginResize(e, WindowEdge.East);

	/// <summary>
	/// Starts native window resize-drag from the top edge.
	/// </summary>
	[RelayCommand]
	private static void ResizeByTop(PointerPressedEventArgs? e) => BeginResize(e, WindowEdge.North);

	/// <summary>
	/// Starts native window resize-drag from the top-left corner.
	/// </summary>
	[RelayCommand]
	private static void ResizeByTopLeft(PointerPressedEventArgs? e) => BeginResize(e, WindowEdge.NorthWest);

	/// <summary>
	/// Starts native window resize-drag from the top-right corner.
	/// </summary>
	[RelayCommand]
	private static void ResizeByTopRight(PointerPressedEventArgs? e) => BeginResize(e, WindowEdge.NorthEast);

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
			_logger.LogException(ex, isAssertDebug: false);
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
	#endregion

	#region Helpers
	/// <summary>
	/// Starts a native window resize-drag towards <paramref name="edge" />. Left button only.
	/// </summary>
	private static void BeginResize(PointerPressedEventArgs? e, WindowEdge edge)
	{
		if (e?.Source is not Visual visual || !e.GetCurrentPoint(visual)
			.Properties
			.IsLeftButtonPressed)
		{
			return;
		}

		visual
			.FindLogicalAncestorOfType<Window>()?
			.BeginResizeDrag(edge, e);
	}

	/// <summary>
	/// Validates <see cref="ClearCommand" />.
	/// </summary>
	private bool CanClear() => _clipboardHistory.Entries.Count > 0;
	#endregion
}
