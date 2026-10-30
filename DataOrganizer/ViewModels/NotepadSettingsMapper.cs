using EditorDocuments.Helpers;
using EditorDocuments.Interfaces;
using Settings.Dto;
using System.Collections.Generic;
using System.Linq;
using TextProcessing.Interfaces;

namespace DataOrganizer.ViewModels;

/// <summary>
/// Carries the notepad tabs between their settings and the tabs themselves.
/// </summary>
internal static class NotepadSettingsMapper
{
	#region Methods
	/// <summary>
	/// Returns the settings that keep the tabs in their order, with the number of the selected one.
	/// </summary>
	public static NotepadViewSettings Create(IEnumerable<NotepadTabViewModel> tabs, NotepadTabViewModel? selectedTab)
	{
		return new()
		{
			SelectedTabNumber = selectedTab?.Number,
			Tabs = [.. tabs.Select(CreateTabSettings)]
		};
	}

	/// <summary>
	/// Returns a tab of a text with the name, the editor state and the split kept in its settings; the text is read later,
	/// in the encoding kept with them.
	/// </summary>
	public static NotepadTabViewModel CreateTab(
		NotepadTabSettings settings,
		IDocumentText document,
		ISyntaxRegistry syntaxRegistry)
	{
		NotepadTabViewModel tab = new(document)
		{
			Name = settings.Name,
			Number = settings.Number
		};

		if (settings.Split is { } split)
		{
			tab.Editor.SplitShare = split;

			tab.Editor.IsSplit = true;
		}

		if (settings.EditorState is not { } state)
		{
			return tab;
		}

		EditorStateMapper.Apply(tab.Editor, state, syntaxRegistry);

		// Reading no bytes gives the tab the kept encoding, which its text is read in later.
		tab
			.Editor
			.Codec
			.Read([], state.Encoding);

		return tab;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the settings that keep a tab: its number, name, the state of its editor with the encoding of its text, and
	/// its split.
	/// </summary>
	private static NotepadTabSettings CreateTabSettings(NotepadTabViewModel tab)
	{
		DocumentEditorViewModel editor = tab.Editor;

		return new()
		{
			EditorState = EditorStateMapper.Create(editor, editor.Codec.Encoding, isEncrypted: false),
			Name = tab.Name,
			Number = tab.Number,
			Split = editor.IsSplit ? editor.SplitShare : null
		};
	}
	#endregion
}
