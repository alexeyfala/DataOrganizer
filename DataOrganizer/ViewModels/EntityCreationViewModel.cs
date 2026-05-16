using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using Shared.Interfaces;
using Shared.Properties;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="EntityCreationView" />.
/// </summary>
public sealed partial class EntityCreationViewModel : BooleanAsyncResultViewModel
{
	#region Auto-Generated Properties
	/// <inheritdoc cref="EntityCreationViewSettings.IsDatasetSelected" />
	[ObservableProperty]
	private bool _isDatasetSelected;

	/// <inheritdoc cref="EntityCreationViewSettings.IsFileSelected" />
	[ObservableProperty]
	private bool _isFileSelected;

	/// <inheritdoc cref="EntityCreationViewSettings.IsFolderSelected" />
	[ObservableProperty]
	private bool _isFolderSelected;

	/// <inheritdoc cref="EntityCreationViewSettings.Name" />
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
	private string _name = string.Empty;

	/// <summary>
	/// A hint for <see cref="Name" /> input field.
	/// </summary>
	[ObservableProperty]
	private string _nameInputHint = Strings.Name;
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
		ITaskExceptionHandler handler) : base(app, handler)
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

	#region Service
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
