using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Messages;
using DynamicData;
using DynamicData.Binding;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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
	/// History entries to display (delegated to <see cref="IClipboardHistoryService" />).
	/// </summary>
	public ObservableCollection<ClipboardHistoryEntryBase> Entries => _clipboardHistory.Entries;

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
	public ReadOnlyObservableCollection<ClipboardHistoryEntryBase> VisibleEntries => _visibleEntries;
	#endregion

	#region Data
	/// <inheritdoc cref="IClipboardHistoryService" />
	private readonly IClipboardHistoryService _clipboardHistory;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <summary>
	/// Subscription handle for the search-filter pipeline; disposed in <see cref="Dispose" />.
	/// </summary>
	private readonly IDisposable _filterSubscription;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <summary>
	/// Backing field for <see cref="VisibleEntries" />.
	/// </summary>
	private readonly ReadOnlyObservableCollection<ClipboardHistoryEntryBase> _visibleEntries;
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

		IObservable<IChangeSet<ClipboardHistoryEntryBase>> observable = _clipboardHistory
			.Entries
			.ToObservableChangeSet()
			.Filter(BuildSearchPredicate());

		if (SynchronizationContext.Current is { } context)
		{
			// Marshal filter updates back to the UI thread: the search throttle emits off-thread.
			observable = observable.ObserveOn(context);
		}

		_filterSubscription = observable
			.Bind(out _visibleEntries)
			.Subscribe();
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

	#region Commands
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

	#region Helpers
	/// <summary>
	/// Builds the throttled predicate stream that drives <see cref="VisibleEntries" /> from <see cref="SearchText" />.
	/// The first value is applied immediately; later changes are debounced.
	/// </summary>
	private IObservable<Func<ClipboardHistoryEntryBase, bool>> BuildSearchPredicate()
	{
		return this
			.WhenValueChanged(x => x.SearchText)
			.Publish(stream => stream
				.Take(1)
				.Merge(stream
					.Skip(1)
					.Throttle(TimeSpan.FromMilliseconds(500L))))
			.Select(Predicate);

		static Func<ClipboardHistoryEntryBase, bool> Predicate(string? value)
		{
			return !string.IsNullOrWhiteSpace(value)
				? x => x.SearchableText is { } text && text.Contains(value, StringComparison.OrdinalIgnoreCase)
				: _ => true;
		}
	}

	/// <summary>
	/// Validates <see cref="ClearCommand" />.
	/// </summary>
	private bool CanClear() => _clipboardHistory.Entries.Any(static entry => !entry.IsPinned);
	#endregion
}
