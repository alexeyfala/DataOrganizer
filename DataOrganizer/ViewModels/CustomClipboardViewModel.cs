using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Messages;
using DynamicData.Binding;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>CustomClipboardWindow</c>.
/// </summary>
public sealed partial class CustomClipboardViewModel :
	ObservableObject,
	IRecipient<ClipboardHistoryEntryCountChangedMessage>,
	IDisposable
{
	#region Properties
	/// <summary>
	/// Whether the window stays open on focus loss and after a restore.
	/// </summary>
	[ObservableProperty]
	public partial bool KeepOpen { get; set; }

	/// <summary>
	/// Current search query used to filter the history.
	/// </summary>
	[ObservableProperty]
	public partial string? SearchText { get; set; }

	/// <inheritdoc cref="ClipboardHistoryEntryBase" />
	[ObservableProperty]
	public partial ClipboardHistoryEntryBase? SelectedEntry { get; set; }

	/// <summary>
	/// History entries matching the current search query.
	/// </summary>
	public ReadOnlyObservableCollection<ClipboardHistoryEntryBase> VisibleEntries { get; }
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Clears the history list. Disabled while it is empty.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanClear))]
	private Task Clear() => _clipboardHistory.ClearAsync();

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
			: _clipboardHistory.RestoreAsync(entry, keepPosition: KeepOpen);
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

	#region Data
	/// <inheritdoc cref="IClipboardHistoryService" />
	private readonly IClipboardHistoryService _clipboardHistory;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <summary>
	/// Subscription handle for the search-filter pipeline.
	/// </summary>
	private readonly IDisposable _filterSubscription;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <summary>
	/// Backing collection for <see cref="VisibleEntries" />.
	/// </summary>
	private readonly ObservableCollection<ClipboardHistoryEntryBase> _visibleEntries = [];
	#endregion

	#region Constructors
	public CustomClipboardViewModel(
		IClipboardHistoryService clipboardHistory,
		IDispatcherAccessor dispatcher,
		ILogger logger,
		IMessenger messenger)
	{
		_clipboardHistory = clipboardHistory;

		_dispatcher = dispatcher;

		_logger = logger;

		_messenger = messenger;

		messenger.RegisterAll(this);

		VisibleEntries = new(_visibleEntries);

		IObservable<Unit> trigger = _clipboardHistory
			.Entries
			.ToObservableChangeSet()
			.Select(static _ => Unit.Default)
			.Merge(BuildSearchTrigger());

		if (SynchronizationContext.Current is { } context)
		{
			// The search throttle emits off-thread; marshal refreshes back to the UI thread.
			trigger = trigger.ObserveOn(context);
		}

		_filterSubscription = trigger.Subscribe(_ => RefreshVisibleEntries());
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose()
	{
		_filterSubscription.Dispose();

		_messenger.UnregisterAll(this);
	}

	/// <inheritdoc />
	public void Receive(ClipboardHistoryEntryCountChangedMessage message)
	{
		_dispatcher.Post(ClearCommand.NotifyCanExecuteChanged);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds the entry predicate for a search <paramref name="value" />: a blank value matches every entry,
	/// otherwise an entry matches when its <see cref="ClipboardHistoryEntryBase.SearchableText" /> contains it (case-insensitive).
	/// </summary>
	internal static Func<ClipboardHistoryEntryBase, bool> BuildPredicate(string? value)
	{
		return string.IsNullOrWhiteSpace(value)
			? _ => true
			: entry => entry.SearchableText is { } text && text.Contains(value, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Emits when the search query should be re-applied: immediately for the first value, debounced thereafter.
	/// </summary>
	private IObservable<Unit> BuildSearchTrigger()
	{
		return this
			.WhenValueChanged(x => x.SearchText)
			.Publish(stream => stream
				.Take(1)
				.Merge(stream
					.Skip(1)
					.Throttle(TimeSpan.FromMilliseconds(500L))))
			.Select(static _ => Unit.Default);
	}

	/// <summary>
	/// Validates <see cref="ClearCommand" />.
	/// </summary>
	private bool CanClear() => _clipboardHistory.Entries.Any(static entry => !entry.IsPinned);

	/// <summary>
	/// Reconciles <see cref="VisibleEntries" /> with the matching history entries, preserving source order.
	/// </summary>
	private void RefreshVisibleEntries()
	{
		Func<ClipboardHistoryEntryBase, bool> predicate = BuildPredicate(SearchText);

		int target = 0;

		foreach (ClipboardHistoryEntryBase entry in _clipboardHistory.Entries)
		{
			if (!predicate(entry))
			{
				continue;
			}

			int current = _visibleEntries.IndexOf(entry);

			if (current < 0)
			{
				_visibleEntries.Insert(target, entry);
			}
			else if (current != target)
			{
				_visibleEntries.Move(current, target);
			}

			target++;
		}

		// Entries removed from the source or no longer matching pile up past the last match — drop them.
		while (_visibleEntries.Count > target)
		{
			_visibleEntries.RemoveAt(_visibleEntries.Count - 1);
		}
	}
	#endregion
}
