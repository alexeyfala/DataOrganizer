using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Extensions;
using Entities.Models;
using Repository.DTO;
using Shared.Extensions;
using Shared.Properties;
using System.Collections.ObjectModel;

namespace DataOrganizer.DTO.Entities.Models;

/// <inheritdoc cref="FileModel" />
public partial class FileModelDto : ExplorerModelBaseDto
{
	#region Properties
	/// <inheritdoc cref="FileModel.Hotkeys" />
	public ObservableCollection<HotkeyModelDto> Hotkeys { get; init; } = [];

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

	/// <inheritdoc cref="FileModel.IsFavorite" />
	[ObservableProperty]
	public partial bool IsFavorite { get; set; }

	/// <inheritdoc cref="FileModel.Properties" />
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
		CodeMaskPair[] hotkeys = [.. Hotkeys.ToCodeMaskPairs()];

		HotkeysToolTip = hotkeys.IsNotEmpty()
			? $"{Strings.Hotkeys}: {hotkeys.GetHotkeysPresentation()}"
			: null;
	}
	#endregion
}
