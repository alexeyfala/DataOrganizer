using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces.Documents;
using Shared.Properties;
using System.Text;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>NotepadTabView</c>: a tab of the notepad with its text and the state of its editor.
/// </summary>
public sealed partial class NotepadTabViewModel : ObservableObject, IEditorStateValues
{
	#region Properties
	/// <inheritdoc />
	/// <remarks>
	/// None: a notepad text has no file extension to take a language from.
	/// </remarks>
	public string? DefaultSyntaxLanguage => null;

	/// <summary>
	/// Text of the tab as an editable document.
	/// </summary>
	public TextDocument Document { get; } = new();

	/// <inheritdoc cref="FileEditorState.FontSize" />
	[ObservableProperty]
	public partial double FontSize { get; set; } = 14.0;

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
	/// <c>True</c> when the text is shown in two halves, one above the other.
	/// </summary>
	[ObservableProperty]
	public partial bool IsSplit { get; set; }

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
	/// Caret, selection, scroll position, bookmarks and folded blocks of <see cref="Document" />.
	/// </summary>
	[ObservableProperty]
	public partial DocumentViewState? ViewState { get; set; }

	/// <inheritdoc cref="FileEditorState.WordWrap" />
	[ObservableProperty]
	public partial bool WordWrap { get; set; }

	/// <summary>
	/// Encoding the text is kept in on the disk, UTF-8 for a new text.
	/// </summary>
	internal DocumentCodec Codec { get; } = CreateCodec();
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the encoding of a text that has no bytes yet, which is UTF-8.
	/// </summary>
	private static DocumentCodec CreateCodec()
	{
		DocumentCodec codec = new();

		codec.Read([], Encoding.UTF8.WebName);

		return codec;
	}
	#endregion
}
