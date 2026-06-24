using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.Enums.Clipboard;
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
/// View model for <c>ClipboardLogWindow</c>.
/// </summary>
public sealed partial class ClipboardLogViewModel :
	ObservableObject,
	IRecipient<ClipboardLogEntryCountChangedMessage>,
	IDisposable
{
	#region Properties
	/// <summary>
	/// Active type filter applied to the history list.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsSearchEnabled))]
	public partial ClipboardLogEntryFilter ActiveFilter { get; set; }

	/// <summary>
	/// <c>True</c> while the active filter contains entries that carry searchable text
	/// (images and files have none, so search is disabled for them).
	/// </summary>
	public bool IsSearchEnabled => ActiveFilter is not (ClipboardLogEntryFilter.Image or ClipboardLogEntryFilter.Files);

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

	/// <inheritdoc cref="ClipboardLogEntryBase" />
	[ObservableProperty]
	public partial ClipboardLogEntryBase? SelectedEntry { get; set; }

	/// <summary>
	/// History entries matching the current search query.
	/// </summary>
	public ReadOnlyObservableCollection<ClipboardLogEntryBase> VisibleEntries { get; }
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Clears the history list. Disabled while it is empty.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanClear))]
	private Task Clear() => _clipboardLog.ClearAsync();

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
	/// Removes <paramref name="entry" /> from the history.
	/// </summary>
	[RelayCommand]
	private Task RemoveEntry(ClipboardLogEntryBase? entry)
	{
		if (entry is null)
		{
			return Task.CompletedTask;
		}

		return _clipboardLog.RemoveAsync(entry);
	}

	/// <summary>
	/// Restores <paramref name="entry" /> back into the system clipboard.
	/// </summary>
	[RelayCommand(AllowConcurrentExecutions = true)]
	private Task RestoreEntry(ClipboardLogEntryBase? entry)
	{
		return entry is null
			? Task.CompletedTask
			: _clipboardLog.RestoreAsync(entry, keepPosition: KeepOpen);
	}

	/// <summary>
	/// Toggles the pinned state of <paramref name="entry" />.
	/// </summary>
	[RelayCommand]
	private void TogglePin(ClipboardLogEntryBase? entry)
	{
		if (entry is null)
		{
			return;
		}

		_clipboardLog.TogglePin(entry);

		ClearCommand.NotifyCanExecuteChanged();
	}
	#endregion

	#region Partial
	/// <summary>
	/// Stashes the search query when switching to a filter whose entries have no searchable text (so a
	/// leftover query does not leave the list empty), and restores it when returning to a searchable filter.
	/// </summary>
	partial void OnActiveFilterChanged(ClipboardLogEntryFilter value)
	{
		if (IsSearchEnabled)
		{
			if (_stashedSearchText is { } stashed)
			{
				SearchText = stashed;

				_stashedSearchText = null;
			}
		}
		else if (SearchText is { } current)
		{
			_stashedSearchText = current;

			SearchText = null;
		}
	}
	#endregion

	#region Data
	/// <inheritdoc cref="IClipboardLogService" />
	private readonly IClipboardLogService _clipboardLog;

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
	private readonly ObservableCollection<ClipboardLogEntryBase> _visibleEntries = [];

	/// <summary>
	/// Search query held aside while a non-searchable filter is active, restored on return.
	/// </summary>
	private string? _stashedSearchText;
	#endregion

	#region Constructors
	public ClipboardLogViewModel(
		IClipboardLogService clipboardLog,
		IDispatcherAccessor dispatcher,
		ILogger logger,
		IMessenger messenger)
	{
		_clipboardLog = clipboardLog;

		_dispatcher = dispatcher;

		_logger = logger;

		_messenger = messenger;

		messenger.RegisterAll(this);

		VisibleEntries = new(_visibleEntries);

		IObservable<Unit> trigger = clipboardLog
			.Entries
			.ToObservableChangeSet()
			.Select(static _ => Unit.Default)
			.Merge(BuildSearchTrigger())
			.Merge(BuildFilterTrigger());

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
	public void Receive(ClipboardLogEntryCountChangedMessage message)
	{
		_dispatcher.Post(ClearCommand.NotifyCanExecuteChanged);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds the entry predicate for a search <paramref name="value" />: a blank value matches every entry,
	/// otherwise an entry matches when its <see cref="ClipboardLogEntryBase.SearchableText" /> contains it (case-insensitive).
	/// </summary>
	internal static Func<ClipboardLogEntryBase, bool> BuildSearchPredicate(string? value)
	{
		return string.IsNullOrWhiteSpace(value)
			? _ => true
			: entry => entry.SearchableText is { } text && text.Contains(value, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Builds the entry predicate for a type <paramref name="filter" />: <see cref="ClipboardLogEntryFilter.All" />
	/// matches every entry, otherwise only entries of the matching payload type (text excludes URLs).
	/// </summary>
	internal static Func<ClipboardLogEntryBase, bool> BuildTypePredicate(ClipboardLogEntryFilter filter)
	{
		return filter switch
		{
			ClipboardLogEntryFilter.Text => static entry => entry is ClipboardTextEntry and not ClipboardUrlEntry,
			ClipboardLogEntryFilter.Url => static entry => entry is ClipboardUrlEntry,
			ClipboardLogEntryFilter.Image => static entry => entry is ClipboardImageEntry,
			ClipboardLogEntryFilter.Files => static entry => entry is ClipboardFilesEntry,
			_ => static _ => true
		};
	}

	/// <summary>
	/// Emits when the active type filter changes.
	/// </summary>
	private IObservable<Unit> BuildFilterTrigger()
	{
		return this
			.WhenValueChanged(x => x.ActiveFilter)
			.Select(static _ => Unit.Default);
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
	private bool CanClear() => _clipboardLog.Entries.Any(static entry => !entry.IsPinned);

	/// <summary>
	/// Reconciles <see cref="VisibleEntries" /> with the matching history entries, preserving source order.
	/// </summary>
	private void RefreshVisibleEntries()
	{
		Func<ClipboardLogEntryBase, bool> searchPredicate = BuildSearchPredicate(SearchText);

		Func<ClipboardLogEntryBase, bool> typePredicate = BuildTypePredicate(ActiveFilter);

		int target = 0;

		foreach (ClipboardLogEntryBase entry in _clipboardLog.Entries)
		{
			if (!typePredicate(entry) || !searchPredicate(entry))
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
