using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Documents;
using DataOrganizer.Interfaces.Documents;
using System.Collections.Generic;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model of what <c>DocumentEditorView</c> shows and edits: one document for the whole life of the editor, the
/// encoding of its text and the switches of the editor.
/// </summary>
public sealed partial class DocumentEditorViewModel : ObservableObject, IEditorStateValues, IEncodedDocument
{
	#region Properties
	/// <inheritdoc />
	public DocumentCodec Codec { get; } = new();

	/// <summary>
	/// Web name of the encoding that the bytes of the text are found to be in; <c>null</c> until they are read, or when they
	/// are not found to be text.
	/// </summary>
	[ObservableProperty]
	public partial string? DefaultEncoding { get; private set; }

	/// <inheritdoc />
	[ObservableProperty]
	public partial string? DefaultSyntaxLanguage { get; internal set; }

	/// <summary>
	/// The text as an editable document.
	/// </summary>
	public IDocumentText Document { get; }

	/// <summary>
	/// Web name of the encoding the text is read in and written in; another one reads the text again from the same bytes.
	/// </summary>
	[ObservableProperty]
	public partial string? Encoding { get; set; }

	/// <summary>
	/// Name of <see cref="Encoding" /> as the status bar shows it; <c>null</c> until the text is read.
	/// </summary>
	[ObservableProperty]
	public partial string? EncodingName { get; private set; }

	/// <inheritdoc cref="FileEditorState.FontSize" />
	[ObservableProperty]
	public partial double FontSize { get; set; } = 14.0;

	/// <summary>
	/// <c>True</c> when the text is shown in two halves, one above the other.
	/// </summary>
	[ObservableProperty]
	public partial bool IsSplit { get; set; }

	/// <inheritdoc cref="FileEditorState.ShowEndOfLine" />
	[ObservableProperty]
	public partial bool ShowEndOfLine { get; set; }

	/// <inheritdoc cref="FileEditorState.ShowSpaces" />
	[ObservableProperty]
	public partial bool ShowSpaces { get; set; }

	/// <inheritdoc cref="FileEditorState.ShowTabs" />
	[ObservableProperty]
	public partial bool ShowTabs { get; set; }

	/// <summary>
	/// Share of the height that the upper half takes while the text is split.
	/// </summary>
	[ObservableProperty]
	public partial double SplitShare { get; set; } = 0.5;

	/// <summary>
	/// Language of the text for the syntax highlighting; <c>null</c> for plain text.
	/// </summary>
	[ObservableProperty]
	public partial string? SyntaxLanguage { get; set; }

	/// <summary>
	/// Web names of the encodings that cannot read the text as its bytes are; <c>null</c> when they are not known.
	/// </summary>
	[ObservableProperty]
	public partial IReadOnlyCollection<string>? UnreadableEncodings { get; private set; }

	/// <summary>
	/// Caret, selection, scroll position, bookmarks and folded blocks of <see cref="Document" />.
	/// </summary>
	[ObservableProperty]
	public partial DocumentViewState? ViewState { get; set; }

	/// <inheritdoc cref="FileEditorState.WordWrap" />
	[ObservableProperty]
	public partial bool WordWrap { get; set; }
	#endregion

	#region Constructors
	public DocumentEditorViewModel(IDocumentText document) => Document = document;
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Finds <see cref="UnreadableEncodings" /> for the text as it is now.
	/// </summary>
	[RelayCommand]
	internal void FindUnreadableEncodings() => UnreadableEncodings = Codec.FindUnreadableEncodings(Document.Text);
	#endregion

	#region Methods
	/// <inheritdoc />
	public void RefreshEncoding()
	{
		DefaultEncoding = Codec.DefaultEncoding;

		Encoding = Codec.Encoding;

		EncodingName = Codec.EncodingName;
	}

	/// <inheritdoc />
	public void ReplaceText(string text)
	{
		Document.Replace(text);

		RefreshEncoding();
	}
	#endregion
}
