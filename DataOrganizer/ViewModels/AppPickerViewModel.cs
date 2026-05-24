using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.DTO;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="AppPickerView" />. Lets the user choose one application
/// from the supplied <see cref="Candidates" /> list to open a file with.
/// </summary>
internal sealed partial class AppPickerViewModel : AsyncResultViewModelBase<AssociatedAppInfo?>
{
	#region Properties
	/// <summary>
	/// Applications available to open the file.
	/// </summary>
	public ObservableCollection<AssociatedAppInfo> Candidates { get; } = [];

	/// <summary>
	/// Dialog header text shown above the list.
	/// </summary>
	[ObservableProperty]
	public partial string? Header { get; set; }

	/// <summary>
	/// Currently selected application; two-way bound to the ListBox.
	/// </summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(OpenCommand))]
	public partial AssociatedAppInfo? SelectedApp { get; set; }
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Cancels the dialog without choosing.
	/// </summary>
	[RelayCommand]
	private Task Cancel() => SetResultAsync(null);

	/// <summary>
	/// Confirms the selected application and closes the dialog.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanOpen))]
	private Task Open() => SetResultAsync(SelectedApp);
	#endregion

	#region Constructors
	public AppPickerViewModel(Application app, ITaskExceptionHandler handler) : base(app, handler)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc cref="AsyncResultViewModelBase{TResult}.GetResultAsync" />
	public Task<AssociatedAppInfo?> GetResultAsync(CancellationToken token = default) =>
		GetResultAsync(defaultResult: null, token: token);
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="OpenCommand" />.
	/// </summary>
	private bool CanOpen() => SelectedApp is not null;
	#endregion
}
