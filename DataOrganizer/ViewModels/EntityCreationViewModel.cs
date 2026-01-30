using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Abstract;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Views;
using Shared.Common;
using Shared.Interfaces;
using Shared.Properties;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="EntityCreationView" />.
/// </summary>
public sealed partial class EntityCreationViewModel : DefaultButtonViewModelBase
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
	[NotifyCanExecuteChangedFor(nameof(DefaultPressedCommand))]
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

	#region Data
	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IJsonSerializerWrapper" />
	private readonly IJsonSerializerWrapper _jsonSerializer;
	#endregion

	#region Constructors
	public EntityCreationViewModel(IFileSystem fileSystem, IJsonSerializerWrapper jsonSerializer)
	{
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
			AppUtils.GetSettingsFilePath(nameof(EntityCreationViewSettings)),
			false);
	}

	/// <inheritdoc />
	protected override bool CanExecuteDefaultPressed() => !string.IsNullOrWhiteSpace(Name);
	#endregion

	#region Service
	/// <summary>
	/// Initializes settings from file.
	/// </summary>
	private void InitializeFromFile()
	{
		string filePath = AppUtils.GetSettingsFilePath(nameof(EntityCreationViewSettings));

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
