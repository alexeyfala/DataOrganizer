using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces;
using Serilog;
using Shared.Extensions;
using System.Collections.ObjectModel;

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
	/// Index of selected element in <see cref="TabControl" />.
	/// </summary>
	[ObservableProperty]
	public partial int SelectedIndex { get; set; }
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
	#endregion
}
