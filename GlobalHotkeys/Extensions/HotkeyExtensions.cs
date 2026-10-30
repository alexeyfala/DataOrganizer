using Cysharp.Text;
using Hierarchy.Dto;
using Hierarchy.Extensions;
using Repository.Dto;
using Shared.Extensions;
using Shared.Properties;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GlobalHotkeys.Extensions;

public static class HotkeyExtensions
{
	#region Methods
	/// <summary>
	/// Filters a hierarchical sequence of <see cref="ExplorerItemDtoBase" /> by hotkeys that could not be read.
	/// </summary>
	/// <returns>Flat sequence <see cref="FileDto" />.</returns>
	public static IEnumerable<FileDto> GetFilesWithUnreadableHotkeys(
		this IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		return hierarchy.GetFilesBy(x => x.Hotkeys.Any(IsUnreadable));
	}

	/// <summary>
	/// Returns a string representation of the sequence <see cref="HotkeyDto" />.
	/// </summary>
	public static string GetHotkeysPresentation(this KeyStroke[] hotkeys)
	{
		using Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();

		if (hotkeys.Length != 0 && hotkeys[0].Mask.IsNotDefault())
		{
			builder.Append(hotkeys[0].Mask);

			builder.Append(' ');

			builder.Append('+');

			builder.Append(' ');
		}

		for (int i = 0; i < hotkeys.Length; i++)
		{
			builder.Append(hotkeys[i].GetKeyName());

			if (i == hotkeys.Length - 1)
			{
				continue;
			}

			builder.Append(',');

			builder.Append(' ');
		}

		return builder.ToString();
	}

	/// <summary>
	/// Builds the text that names the files whose hotkeys could not be read, a file per line,
	/// under the given <paramref name="header"/>.
	/// </summary>
	public static string GetUnreadableHotkeysPresentation(this FileDto[] files, string header)
	{
		const int maxNames = 3;

		using Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();

		builder.Append(header);

		builder.Append(':');

		int names = Math.Min(files.Length, maxNames);

		for (int i = 0; i < names; i++)
		{
			builder.Append(Environment.NewLine);

			builder.Append('"');

			builder.Append(files[i].Name);

			builder.Append('"');
		}

		if (files.Length > maxNames)
		{
			builder.Append(Environment.NewLine);

			builder.AppendFormat(Strings.AndMore, files.Length - maxNames);
		}

		return builder.ToString();
	}

	/// <summary>
	/// Transforms a sequence of <see cref="KeyStroke" /> to a sequence of <see cref="HotkeyDto" />.
	/// </summary>
	public static IEnumerable<HotkeyDto> ToHotkeyDtos(
		this KeyStroke[] hotkeys,
		Guid id = default,
		Guid ownerId = default)
	{
		for (int i = 0; i < hotkeys.Length; i++)
		{
			KeyStroke hotkey = hotkeys[i];

			yield return new()
			{
				Code = hotkey.Code,
				Id = id,
				Index = i,
				Mask = hotkey.Mask,
				OwnerId = ownerId
			};
		}
	}

	/// <summary>
	/// Transforms a sequence of <see cref="HotkeyDto" /> to a sequence of <see cref="KeyStroke" />.
	/// </summary>
	public static IEnumerable<KeyStroke> ToKeyStrokes(this IEnumerable<HotkeyDto> sequence)
	{
		return sequence.Select(x => new KeyStroke
		{
			Code = x.Code,
			Mask = x.Mask
		});
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when a hotkey holds a key or a mask that the library no longer has.
	/// </summary>
	private static bool IsUnreadable(HotkeyDto hotkey)
	{
		return hotkey.Code == KeyCode.VcUndefined || hotkey.Mask == EventMask.None;
	}
	#endregion
}
