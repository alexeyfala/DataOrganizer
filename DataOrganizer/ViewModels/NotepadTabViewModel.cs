using CommunityToolkit.Mvvm.ComponentModel;
using EditorDocuments.Interfaces;
using NotepadDocuments.Interfaces;
using Shared.Properties;
using System.Text;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>NotepadTabView</c>: a tab of the notepad with its text and the state of its editor.
/// </summary>
public sealed partial class NotepadTabViewModel : ObservableObject, INotepadTab
{
	#region Properties
	/// <summary>
	/// Text of the tab with its encoding, UTF-8 for a new text, and the switches of its editor; a notepad text has no file
	/// extension to take a language from.
	/// </summary>
	public DocumentEditorViewModel Editor { get; }

	/// <summary>
	/// Header: <see cref="Name" />, or <see cref="Strings.New" /> with <see cref="Number" /> while the tab has no name.
	/// </summary>
	public string Header => Name ?? $"{Strings.New} {Number}";

	/// <summary>
	/// <c>True</c> when the text cannot be read from the disk, so it is neither edited nor written back.
	/// </summary>
	[ObservableProperty]
	public partial bool IsReadOnly { get; set; }

	/// <summary>
	/// Name given to the tab, or <c>null</c>.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Header))]
	public partial string? Name { get; set; }

	/// <summary>
	/// Number of the tab, unique among the open tabs.
	/// </summary>
	public required int Number { get; init; }

	/// <inheritdoc />
	IEncodedDocument INotepadTab.Editor => Editor;
	#endregion

	#region Constructors
	public NotepadTabViewModel(IDocumentText document) => Editor = CreateEditor(document);
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the editor of a text that has no bytes yet, which is in UTF-8.
	/// </summary>
	private static DocumentEditorViewModel CreateEditor(IDocumentText document)
	{
		DocumentEditorViewModel editor = new(document);

		editor.Codec.Read([], Encoding.UTF8.WebName);

		return editor;
	}
	#endregion
}
