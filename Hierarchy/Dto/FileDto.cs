using CommunityToolkit.Mvvm.ComponentModel;
using Entities.Models;
using System.Collections.ObjectModel;

namespace Hierarchy.Dto;

/// <inheritdoc cref="FileEntity" />
public sealed partial class FileDto : ExplorerItemDtoBase
{
	#region Properties
	/// <inheritdoc cref="FileEntity.EditorState" />
	public string? EditorState { get; set; }

	/// <inheritdoc cref="FileEntity.Hotkeys" />
	public ObservableCollection<HotkeyDto> Hotkeys { get; init; } = [];

	/// <summary>
	/// <c>True</c> when the file is opened in the built-in editor.
	/// </summary>
	[ObservableProperty]
	public partial bool IsEditing { get; set; }

	/// <summary>
	/// <c>True</c> when the file is executing in the operating system.
	/// </summary>
	[ObservableProperty]
	public partial bool IsExecuting { get; set; }

	/// <inheritdoc cref="FileEntity.IsFavorite" />
	[ObservableProperty]
	public partial bool IsFavorite { get; set; }
	#endregion

	#region Methods
	/// <summary>
	/// <c>True</c> when <see cref="IsEditing" /> == <c>True</c> or <see cref="IsExecuting" /> == <c>True</c>.
	/// </summary>
	public bool IsOpened() => IsEditing || IsExecuting;
	#endregion
}
