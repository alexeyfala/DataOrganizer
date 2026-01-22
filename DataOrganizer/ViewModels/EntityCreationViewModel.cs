using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Abstract;
using DataOrganizer.DTO.Settings;
using DataOrganizer.Views;
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

	#region Methods
	/// <summary>
	/// Performs initialization.
	/// </summary>
	public void Initialize(in EntityCreationViewSettings settings)
	{
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

		IsInitialized = true;
	}

	/// <inheritdoc />
	protected override bool CanExecuteDefaultPressed() => !string.IsNullOrWhiteSpace(Name);
	#endregion
}
