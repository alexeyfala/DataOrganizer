using Avalonia;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.DTO;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using DataOrganizer.Windows;
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
	/// Opens the OS file picker filtered to executables; if the user picks one, the
	/// dialog closes with that selection.
	/// </summary>
	[RelayCommand]
	private async Task Browse()
	{
		FilePickerOpenOptions options = new()
		{
			Title = "Select application",
			AllowMultiple = false,
			FileTypeFilter =
			[
				new FilePickerFileType("Executable")
				{
					Patterns = ["*.exe"]
				}
			]
		};

		string[] filePaths = await _fileSystemPicker
			.SelectFilesAsync<EditorWindow>(options)
			.ConfigureAwait(true);

		if (filePaths.Length == 0)
		{
			return;
		}

		if (_appPicker.CreateFromPath(filePaths[0]) is not { } info)
		{
			return;
		}

		await SetResultAsync(info).ConfigureAwait(false);
	}

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

	#region Data
	/// <inheritdoc cref="IAppPickerService" />
	private readonly IAppPickerService _appPicker;

	/// <inheritdoc cref="IFileSystemPicker" />
	private readonly IFileSystemPicker _fileSystemPicker;
	#endregion

	#region Constructors
	public AppPickerViewModel(
		Application app,
		IAppPickerService appPicker,
		IFileSystemPicker fileSystemPicker,
		ITaskExceptionHandler handler) : base(app, handler)
	{
		_appPicker = appPicker;

		_fileSystemPicker = fileSystemPicker;
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
