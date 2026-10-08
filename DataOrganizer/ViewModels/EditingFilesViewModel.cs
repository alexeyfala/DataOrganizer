using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces;
using Serilog;
using Shared.Extensions;
using System.Collections.ObjectModel;
using System.Linq;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>EditingFilesView</c>.
/// </summary>
public sealed partial class EditingFilesViewModel : ObservableObject
{
	#region Properties
	/// <summary>
	/// Files opened in the editor.
	/// </summary>
	public ObservableCollection<FileDto> Items { get; } = [];

	/// <summary>
	/// File of the tab Ctrl+Tab goes back to.
	/// </summary>
	[ObservableProperty]
	public partial FileDto? PreviousFile { get; set; }

	/// <summary>
	/// File of the selected tab.
	/// </summary>
	public FileDto? SelectedFile => Items.ElementAtOrDefault(SelectedIndex);

	/// <summary>
	/// Index of selected element in <see cref="TabControl" />.
	/// </summary>
	[ObservableProperty]
	public partial int SelectedIndex { get; set; }

	/// <summary>
	/// Files of the tabs with the selected one and the one Ctrl+Tab goes back to.
	/// </summary>
	public EditorTabsState State => new()
	{
		Files = [.. Items],
		// A closed file is no way back.
		PreviousFile = PreviousFile is { } file && Items.Contains(file) ? file : null,
		SelectedFile = SelectedFile
	};
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Closes a the tab in <see cref="TabControl" />.
	/// </summary>
	[RelayCommand]
	internal void CloseTab(FileDto dto)
	{
		if (dto is null)
		{
			return;
		}

		_logger.LogInformation($"Closing opened in editor file:{dto.GetPropertyValues(
			true,
			nameof(FileDto.Id),
			nameof(FileDto.Name),
			nameof(FileDto.Kind))}");

		dto.IsEditing = false;

		CloseEditor(dto);
	}
	#endregion

	#region Data
	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IViewCache" />
	private readonly IViewCache _viewCache;
	#endregion

	#region Constructors
	public EditingFilesViewModel(ILogger logger, IViewCache viewCache)
	{
		_logger = logger;

		_viewCache = viewCache;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Closes editor associated with the object.
	/// </summary>
	public void CloseEditor(FileDto dto)
	{
		Items.Remove(dto);

		_viewCache.Remove(dto);
	}

	/// <summary>
	/// Opens a file in built-in the editor.
	/// </summary>
	public void OpenInEditor(FileDto dto)
	{
		if (dto is null)
		{
			return;
		}

		if (dto.IsEditing)
		{
			_logger.LogWarning($"The file is already opened in the built-in editor:{dto.GetPropertyValues(
				true,
				nameof(FileDto.Id),
				nameof(FileDto.Name),
				nameof(FileDto.Kind))}");

			return;
		}

		_logger.LogInformation($"The file needs to be opened in the built-in editor:{dto.GetPropertyValues(
			true,
			nameof(FileDto.Id),
			nameof(FileDto.Name))}");

		dto.IsEditing = true;

		Items.Add(dto);

		SelectedIndex = Items.Count - 1;
	}

	/// <summary>
	/// Opens the files of the tabs, selects the tab of the selected file and gives Ctrl+Tab its way back.
	/// </summary>
	public void Restore(EditorTabsState state)
	{
		Items.AddRange(state.Files);

		if (state.SelectedFile is { } selected && Items.Contains(selected))
		{
			SelectedIndex = Items.IndexOf(selected);
		}

		// After the selection, so the tabs it passes on its way do not take the place of the previous one.
		PreviousFile = state.PreviousFile;
	}
	#endregion
}
