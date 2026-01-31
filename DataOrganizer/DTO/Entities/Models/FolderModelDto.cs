using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.DTO.Entities.Abstract;
using Entities.Models;
using System;
using System.Collections.ObjectModel;

namespace DataOrganizer.DTO.Entities.Models;

/// <inheritdoc cref="FolderModel" />
public sealed partial class FolderModelDto : ExplorerModelBaseDto
{
	#region Properties
	/// <inheritdoc cref="FolderModel.Children" />
	public ObservableCollection<ExplorerModelBaseDto> Children { get; } = [];

	/// <inheritdoc cref="FolderModel.PasswordHash" />
	public string? PasswordHash { get; set; }
	#endregion

	#region Auto-Generated Properties
	/// <inheritdoc cref="FolderModel.IsExpanded" />
	[ObservableProperty]
	private bool _isExpanded;
	#endregion

	#region Events
	/// <summary>
	/// Occurs when <see cref="IsExpanded" /> changes.
	/// </summary>
	public static event EventHandler<FolderModelDto>? IsExpandedChanged;
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="IsExpanded" /> changes.
	/// </summary>
	partial void OnIsExpandedChanged(bool value)
	{
		if (this.Id == default)
		{
			return;
		}

		IsExpandedChanged?.Invoke(this, this);
	}
	#endregion	
}
