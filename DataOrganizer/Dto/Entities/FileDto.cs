using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Extensions;
using Entities.Models;
using Repository.Dto;
using Shared.Extensions;
using Shared.Properties;
using System.Collections.ObjectModel;

namespace DataOrganizer.Dto.Entities;

/// <inheritdoc cref="FileEntity" />
public sealed partial class FileDto : ExplorerItemDtoBase
{
	#region Properties
	/// <inheritdoc cref="FileEntity.Hotkeys" />
	public ObservableCollection<HotkeyDto> Hotkeys { get; init; } = [];

	/// <summary>
	/// A tooltip for hotkeys.
	/// </summary>
	[ObservableProperty]
	public partial string? HotkeysToolTip { get; set; }

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

	/// <inheritdoc cref="FileEntity.Properties" />
	public string? Properties { get; set; }
	#endregion

	#region Methods
	/// <summary>
	/// <c>True</c> when <see cref="IsEditing" /> == <c>True</c> or <see cref="IsExecuting" /> == <c>True</c>.
	/// </summary>
	public bool IsOpened() => IsEditing || IsExecuting;

	/// <summary>
	/// Sets <see cref="HotkeysToolTip" /> according to <see cref="Hotkeys" />.
	/// </summary>
	public void SetHotkeysToolTip()
	{
		KeyStroke[] hotkeys = [.. Hotkeys.ToKeyStrokes()];

		HotkeysToolTip = hotkeys.IsNotEmpty()
			? $"{Strings.Hotkeys}: {hotkeys.GetHotkeysPresentation()}"
			: null;
	}
	#endregion
}
