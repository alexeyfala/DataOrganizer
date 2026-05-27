using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO;
using DataOrganizer.Interfaces;
using DataOrganizer.Windows;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="SystemClipboardWindow" />.
/// </summary>
public sealed partial class SystemClipboardViewModel : ObservableObject
{
	#region Properties
	/// <summary>
	/// History entries to display (delegated to <see cref="IClipboardHistoryService" />).
	/// </summary>
	public ObservableCollection<ClipboardHistoryEntry> Entries => _history.Entries;

	/// <inheritdoc cref="ClipboardHistoryEntry" />
	[ObservableProperty]
	public partial ClipboardHistoryEntry? SelectedEntry { get; set; }
	#endregion

	#region Data
	/// <inheritdoc cref="IClipboardHistoryService" />
	private readonly IClipboardHistoryService _history;
	#endregion

	#region Constructors
	public SystemClipboardViewModel(IClipboardHistoryService history) => _history = history;
	#endregion

	#region Commands
	/// <summary>
	/// Restores <paramref name="entry" /> back into the system clipboard.
	/// </summary>
	[RelayCommand]
	private Task RestoreEntry(ClipboardHistoryEntry? entry)
	{
		return entry is null
			? Task.CompletedTask
			: _history.RestoreAsync(entry);
	}
	#endregion
}
