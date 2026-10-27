using Avalonia.Data.Converters;
using DataOrganizer.Helpers.Notes;
using Entities.Enums;
using GlobalHotkeys.Extensions;
using Hierarchy.Dto;
using Material.Icons;
using Repository.Dto;
using Shared.Properties;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Converters;

/// <summary>
/// Stateless one-way value converters of the objects of the tree, referenced via x:Static.
/// </summary>
internal static class ExplorerConverters
{
	#region Properties
	/// <summary>
	/// Icon of the kind of an object.
	/// </summary>
	public static FuncValueConverter<EntityKind, MaterialIconKind> EntityKindToIconKind { get; } =
		new(kind => kind switch
		{
			EntityKind.Folder => MaterialIconKind.Folder,
			EntityKind.File => MaterialIconKind.FileOutline,
			EntityKind.Dataset => MaterialIconKind.ViewSplitHorizontal,
			_ => default
		});

	/// <summary>
	/// Tooltip of the hotkeys of a file; <c>null</c> for a file without them.
	/// Inputs: [Hotkeys, Hotkeys.Count]; the count only makes the binding read the changed hotkeys again.
	/// </summary>
	public static FuncMultiValueConverter<object?, string?> HotkeysToToolTip { get; } = new(GetHotkeysToolTip);

	/// <inheritdoc cref="NoteHeaderBuilder.Build" />
	public static FuncValueConverter<string?, string?> NoteHeader { get; } = new(NoteHeaderBuilder.Build);
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the tooltip of the hotkeys that come first among the values; <c>null</c> when there are none.
	/// </summary>
	private static string? GetHotkeysToolTip(IEnumerable<object?> values)
	{
		if (values.FirstOrDefault() is not IEnumerable<HotkeyDto> hotkeys)
		{
			return null;
		}

		KeyStroke[] keyStrokes = [.. hotkeys.ToKeyStrokes()];

		if (keyStrokes.Length == 0)
		{
			return null;
		}

		return $"{Strings.Hotkeys}: {keyStrokes.GetHotkeysPresentation()}";
	}
	#endregion
}
