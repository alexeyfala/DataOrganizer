using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Settings;
using Shared.Properties;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels.Dialogs;

/// <summary>
/// View model for <c>EntityCreationView</c>.
/// </summary>
public sealed partial class EntityCreationViewModel : BooleanAsyncResultViewModel
{
	#region Properties
	/// <inheritdoc cref="EntityCreationViewSettings.IsDatasetSelected" />
	[ObservableProperty]
	public partial bool IsDatasetSelected { get; set; }

	/// <inheritdoc cref="EntityCreationViewSettings.IsFileSelected" />
	[ObservableProperty]
	public partial bool IsFileSelected { get; set; }

	/// <inheritdoc cref="EntityCreationViewSettings.IsFolderSelected" />
	[ObservableProperty]
	public partial bool IsFolderSelected { get; set; }

	/// <inheritdoc cref="EntityCreationViewSettings.Name" />
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
	public partial string Name { get; set; } = string.Empty;

	/// <summary>
	/// A hint for <see cref="Name" /> input field.
	/// </summary>
	[ObservableProperty]
	public partial string NameInputHint { get; set; } = Strings.Name;
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="IsFileSelected" /> changes.
	/// </summary>
	partial void OnIsFileSelectedChanged(bool value)
	{
		NameInputHint = value
			? Strings.NameAndExtension
			: Strings.Name;
	}
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Closes the dialog without creating anything.
	/// </summary>
	[RelayCommand]
	private Task Cancel() => SetResultAsync(false);

	/// <summary>
	/// Closes the dialog and confirms the creation.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanSave))]
	private Task Save() => SetResultAsync(true);
	#endregion

	#region Data
	/// <inheritdoc cref="IWindowSettingsStore" />
	private readonly IWindowSettingsStore _windowSettings;
	#endregion

	#region Constructors
	public EntityCreationViewModel(
		IDialogCloser dialogCloser,
		ITaskExceptionHandler exceptionHandler,
		IWindowSettingsStore windowSettings) : base(dialogCloser, exceptionHandler)
	{
		_windowSettings = windowSettings;

		LoadSettings();
	}
	#endregion

	#region Methods
	/// <summary>
	/// Saves the settings in a file.
	/// </summary>
	public void SaveSettingsToFile()
	{
		EntityCreationViewSettings settings = new()
		{
			IsDatasetSelected = IsDatasetSelected,
			IsFileSelected = IsFileSelected,
			IsFolderSelected = IsFolderSelected,
			Name = Name
		};

		_windowSettings.Write(settings);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="SaveCommand" />.
	/// </summary>
	private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

	/// <summary>
	/// Initializes the settings from the stored ones.
	/// </summary>
	private void LoadSettings()
	{
		EntityCreationViewSettings settings = _windowSettings.Read<EntityCreationViewSettings>();

		IsFolderSelected = settings.IsFolderSelected;

		IsFileSelected = settings.IsFileSelected;

		IsDatasetSelected = settings.IsDatasetSelected;

		Name = settings.Name;

		if (!IsFolderSelected
			&& !IsFileSelected
			&& !IsDatasetSelected)
		{
			IsFolderSelected = true;
		}
	}
	#endregion
}
