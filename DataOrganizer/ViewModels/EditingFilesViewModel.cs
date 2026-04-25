using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Helpers;
using DataOrganizer.Views;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="EditingFilesView" />.
/// </summary>
public sealed partial class EditingFilesViewModel : ObservableObject
{
	#region Properties
	/// <summary>
	/// Opened in editor files.
	/// </summary>
	public ObservableCollection<FileModelDto> Items { get; } = [];
	#endregion

	#region Auto-Generated Properties
	/// <summary>
	/// Index of selected element in <see cref="TabControl" />.
	/// </summary>
	[ObservableProperty]
	private int _selectedIndex;
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Closes a the tab in <see cref="TabControl" />.
	/// </summary>
	[RelayCommand]
	public void CloseTab(FileModelDto dto)
	{
		if (dto is null)
		{
			return;
		}

		_logger.LogInformation($"Closing opened in editor file:{dto.GetPropertyValues(
			true,
			nameof(FileModelDto.Id),
			nameof(FileModelDto.Name),
			nameof(FileModelDto.EntityType))}");

		dto.IsEditing = false;

		CloseEditor(dto);
	}

	/// <summary>
	/// Handles the <see cref="SelectingItemsControl.SelectionChanged" /> event of <see cref="TabControl" />.
	/// </summary>
	[RelayCommand]
	private async Task SelectionChanged(SelectionChangedEventArgs? e)
	{
		if (e?.Source is not TabControl container || container.SelectedItem is not FileModelDto dto)
		{
			return;
		}

		_logger.LogDebug($@"File selected in ""{nameof(TabControl)}"":{dto.GetPropertyValues(
			true,
			nameof(FileModelDto.Id),
			nameof(FileModelDto.Name))}");

		TabItem? tabItem = null;

		Func<bool> condition = () => (tabItem = container.ContainerFromItem(dto) as TabItem) is not null;

		if (!await condition
			.WaitAsync(100, 10)
			.ConfigureAwait(true) || tabItem is null)
		{
			return;
		}

		// To be able to switch tabs with Ctrl+Tab while adding tab.
		tabItem.Focus();
	}

	/// <summary>
	/// Switches the <see cref="TabControl" /> to previous tab.
	/// </summary>
	[RelayCommand]
	private void SwitchToPreviousTab()
	{
		if (_previousSelectedIndex < 0)
		{
			return;
		}

		SelectedIndex = _previousSelectedIndex;
	}
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="SelectedIndex" /> changes.
	/// </summary>
	partial void OnSelectedIndexChanged(int oldValue, int newValue) => _previousSelectedIndex = oldValue;
	#endregion

	#region Data
	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <summary>
	/// Previous <see cref="SelectedIndex" /> value.
	/// </summary>
	private int _previousSelectedIndex;
	#endregion

	#region Constructors
	public EditingFilesViewModel(ILogger logger) => _logger = logger;
	#endregion

	#region Methods
	/// <summary>
	/// Closes editor associated with the object.
	/// </summary>
	public void CloseEditor(FileModelDto dto)
	{
		Items.Remove(dto);

		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		ViewLocator.RemoveFromCache(dto);
	}

	/// <summary>
	/// Opens a file in built-in the editor.
	/// </summary>
	public void OpenInEditor(FileModelDto dto)
	{
		if (dto is null)
		{
			return;
		}

		if (dto.IsEditing)
		{
			_logger.LogWarning($"The file is already opened in the built-in editor:{dto.GetPropertyValues(
				true,
				nameof(FileModelDto.Id),
				nameof(FileModelDto.Name),
				nameof(FileModelDto.EntityType))}");

			return;
		}

		_logger.LogInformation($"The file needs to be opened in the built-in editor:{dto.GetPropertyValues(
			true,
			nameof(FileModelDto.Id),
			nameof(FileModelDto.Name))}");

		dto.IsEditing = true;

		Items.Add(dto);

		SelectedIndex = Items.Count - 1;
	}
	#endregion
}
