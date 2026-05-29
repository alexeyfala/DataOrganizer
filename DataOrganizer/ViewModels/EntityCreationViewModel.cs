using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces;
using Shared.Interfaces;
using Shared.Properties;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

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
	/// Cancel.
	/// </summary>
	[RelayCommand]
	private Task Cancel() => SetResultAsync(false);

	/// <summary>
	/// Save.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanSave))]
	private Task Save() => SetResultAsync(true);
	#endregion

	#region Data
	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IJsonSerializerWrapper" />
	private readonly IJsonSerializerWrapper _jsonSerializer;
	#endregion

	#region Constructors
	public EntityCreationViewModel(
		Application app,
		IAppEnvironment appEnvironment,
		IFileSystem fileSystem,
		IJsonSerializerWrapper jsonSerializer,
		ITaskExceptionHandler exceptionHandler) : base(app, exceptionHandler)
	{
		_appEnvironment = appEnvironment;

		_fileSystem = fileSystem;

		_jsonSerializer = jsonSerializer;

		InitializeFromFile();
	}
	#endregion

	#region Methods
	/// <summary>
	/// Saves settings in file.
	/// </summary>
	public void SaveSettingsInFile()
	{
		EntityCreationViewSettings settings = new()
		{
			IsDatasetSelected = IsDatasetSelected,
			IsFileSelected = IsFileSelected,
			IsFolderSelected = IsFolderSelected,
			Name = Name
		};

		_fileSystem.SerializeToJsonFile(
			settings,
			_appEnvironment.GetSettingsFilePath(nameof(EntityCreationViewSettings)),
			false);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="SaveCommand" />.
	/// </summary>
	private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

	/// <summary>
	/// Initializes settings from file.
	/// </summary>
	private void InitializeFromFile()
	{
		string filePath = _appEnvironment.GetSettingsFilePath(nameof(EntityCreationViewSettings));

		EntityCreationViewSettings settings = _jsonSerializer.FromFile<EntityCreationViewSettings>(filePath);

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
