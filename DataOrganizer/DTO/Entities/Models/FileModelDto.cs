using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.DTO.Entities.Abstract;
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

	/// <inheritdoc cref="FileModel.Properties" />
	public string? Properties { get; set; }
	#endregion

	#region Auto-Generated Properties
	/// <summary>
	/// A tooltip for hotkeys.
	/// </summary>
	[ObservableProperty]
	private string? _hotkeysToolTip;

	/// <summary>
	/// Returns <c>True</c> if the file is opened in editor.
	/// </summary>
	[ObservableProperty]
	private bool _isEdited;

	/// <summary>
	/// Returns <c>True</c> if the file is executed in the operating system.
	/// </summary>
	[ObservableProperty]
	private bool _isExecuted;

	/// <inheritdoc cref="FileModel.IsFavorite" />
	[ObservableProperty]
	private bool _isFavorite;

	/// <summary>
	/// Order.
	/// </summary>
	/// <remarks>
	/// For filtering purposes.
	/// </remarks>
	[ObservableProperty]
	private int _order;
	#endregion

	#region Methods
	/// <summary>
	/// Returns <c>True</c> if <see cref="IsEdited" /> == <c>True</c> or <see cref="IsExecuted" /> == <c>True</c>.
	/// </summary>
	public bool IsOpened() => IsEdited || IsExecuted;

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
