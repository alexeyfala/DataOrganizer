using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Extensions;
using DataOrganizer.Services.Text;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace DataOrganizer.Views;

/// <summary>
/// Text editor of a <see cref="TextDocument" /> with its toolbar, context menu, status bar and zoom.
/// </summary>
internal sealed partial class DocumentEditorView :
	UserControl,
	IDisposable
{
	#region Properties
	/// <summary>
	/// Web name of the encoding that the text takes when none is chosen; <c>null</c> when there is none.
	/// </summary>
	public string? DefaultEncoding
	{
		get => GetValue(DefaultEncodingProperty);
		set => SetValue(DefaultEncodingProperty, value);
	}

	/// <summary>
	/// Language that the text takes when none is chosen; <c>null</c> for plain text.
	/// </summary>
	public string? DefaultSyntaxLanguage
	{
		get => GetValue(DefaultSyntaxLanguageProperty);
		set => SetValue(DefaultSyntaxLanguageProperty, value);
	}

	/// <summary>
	/// Mark that the list of the languages shows after <see cref="DefaultSyntaxLanguage" />; <c>null</c> for none.
	/// </summary>
	public string? DefaultSyntaxLanguageMark
	{
		get => GetValue(DefaultSyntaxLanguageMarkProperty);
		set => SetValue(DefaultSyntaxLanguageMarkProperty, value);
	}

	/// <summary>
	/// The document being edited.
	/// </summary>
	public TextDocument? Document
	{
		get => GetValue(DocumentProperty);
		set => SetValue(DocumentProperty, value);
	}

	/// <summary>
	/// Font size of the document text, kept apart from <see cref="TemplatedControl.FontSize" />,
	/// which the toolbar inherits.
	/// </summary>
	public double DocumentFontSize
	{
		get => GetValue(DocumentFontSizeProperty);
		set => SetValue(DocumentFontSizeProperty, value);
	}

	/// <summary>
	/// Web name of the encoding the document is stored in; <c>null</c> for a document that is not stored as bytes.
	/// </summary>
	public string? Encoding
	{
		get => GetValue(EncodingProperty);
		set => SetValue(EncodingProperty, value);
	}

	/// <summary>
	/// Name of the encoding the document is stored in; <c>null</c> for a document that is not stored as bytes.
	/// </summary>
	public string? EncodingName
	{
		get => GetValue(EncodingNameProperty);
		set => SetValue(EncodingNameProperty, value);
	}

	/// <summary>
	/// Command that finds <see cref="UnreadableEncodings" /> anew; run as the list of encodings opens.
	/// </summary>
	public ICommand? FindUnreadableEncodingsCommand
	{
		get => GetValue(FindUnreadableEncodingsCommandProperty);
		set => SetValue(FindUnreadableEncodingsCommandProperty, value);
	}

	/// <summary>
	/// <c>True</c> when the document cannot be edited.
	/// </summary>
	public bool IsReadOnly
	{
		get => GetValue(IsReadOnlyProperty);
		set => SetValue(IsReadOnlyProperty, value);
	}

	/// <summary>
	/// <c>True</c> when the text must not appear outside the text area, as the text of an encrypted file.
	/// </summary>
	public bool IsSensitive
	{
		get => GetValue(IsSensitiveProperty);
		set => SetValue(IsSensitiveProperty, value);
	}

	/// <summary>
	/// <c>True</c> when the document is shown in two halves, one above the other.
	/// </summary>
	public bool IsSplit
	{
		get => GetValue(IsSplitProperty);
		set => SetValue(IsSplitProperty, value);
	}

	/// <summary>
	/// Languages offered for the syntax highlighting, plain text first.
	/// </summary>
	public IReadOnlyList<SelectorChoice> LanguageChoices { get; } =
	[
		new SelectorChoice
		{
			Id = null,
			Name = Strings.PlainText,
			SearchTerms = []
		},
		.. SyntaxRegistry.Instance.Languages
	];

	/// <summary>
	/// Line break styles that the document can be brought to, with the names of the styles as their ids.
	/// </summary>
	public IReadOnlyList<SelectorChoice> LineEndingChoices { get; } =
	[
		CreateLineEndingChoice(LineEnding.CrLf),
		CreateLineEndingChoice(LineEnding.Lf),
		CreateLineEndingChoice(LineEnding.Cr)
	];

	/// <summary>
	/// <c>True</c> when line endings are shown.
	/// </summary>
	public bool ShowEndOfLine
	{
		get => GetValue(ShowEndOfLineProperty);
		set => SetValue(ShowEndOfLineProperty, value);
	}

	/// <summary>
	/// <c>True</c> when spaces are shown.
	/// </summary>
	public bool ShowSpaces
	{
		get => GetValue(ShowSpacesProperty);
		set => SetValue(ShowSpacesProperty, value);
	}

	/// <summary>
	/// <c>True</c> when tabs are shown.
	/// </summary>
	public bool ShowTabs
	{
		get => GetValue(ShowTabsProperty);
		set => SetValue(ShowTabsProperty, value);
	}

	/// <summary>
	/// Share of the height that the upper half takes while the document is split.
	/// </summary>
	public double SplitShare
	{
		get => GetValue(SplitShareProperty);
		set => SetValue(SplitShareProperty, value);
	}

	/// <summary>
	/// Language of the text for the syntax highlighting; <c>null</c> for plain text.
	/// </summary>
	public string? SyntaxLanguage
	{
		get => GetValue(SyntaxLanguageProperty);
		set => SetValue(SyntaxLanguageProperty, value);
	}

	/// <summary>
	/// Content placed at the end of the toolbar.
	/// </summary>
	public object? ToolBarContent
	{
		get => GetValue(ToolBarContentProperty);
		set => SetValue(ToolBarContentProperty, value);
	}

	/// <summary>
	/// Web names of the encodings that cannot read the stored bytes of the document; <c>null</c> when none are known.
	/// </summary>
	public IReadOnlyCollection<string>? UnreadableEncodings
	{
		get => GetValue(UnreadableEncodingsProperty);
		set => SetValue(UnreadableEncodingsProperty, value);
	}

	/// <summary>
	/// Caret, selection, scroll position, bookmarks and folded blocks of the document.
	/// A value set from outside is restored once the document has been laid out.
	/// </summary>
	public DocumentViewState? ViewState
	{
		get => GetValue(ViewStateProperty);
		set => SetValue(ViewStateProperty, value);
	}

	/// <summary>
	/// <c>True</c> when long lines are wrapped.
	/// </summary>
	public bool WordWrap
	{
		get => GetValue(WordWrapProperty);
		set => SetValue(WordWrapProperty, value);
	}
	#endregion

	#region Commands
	/// <summary>
	/// Brings every line break of the document to the style of a choice of <see cref="LineEndingChoices" />, by its id.
	/// </summary>
	public RelayCommand<string?> ConvertLineEndingsCommand { get; }
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="DefaultEncoding" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> DefaultEncodingProperty = AvaloniaProperty
		.Register<DocumentEditorView, string?>(name: nameof(DefaultEncoding));

	/// <summary>
	/// Identifies the <see cref="DefaultSyntaxLanguageMark" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> DefaultSyntaxLanguageMarkProperty = AvaloniaProperty
		.Register<DocumentEditorView, string?>(name: nameof(DefaultSyntaxLanguageMark));

	/// <summary>
	/// Identifies the <see cref="DefaultSyntaxLanguage" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> DefaultSyntaxLanguageProperty = AvaloniaProperty
		.Register<DocumentEditorView, string?>(name: nameof(DefaultSyntaxLanguage));

	/// <summary>
	/// Identifies the <see cref="DocumentFontSize" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<double> DocumentFontSizeProperty = AvaloniaProperty
		.Register<DocumentEditorView, double>(
			name: nameof(DocumentFontSize),
			defaultValue: 14.0);

	/// <summary>
	/// Identifies the <see cref="Document" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<TextDocument?> DocumentProperty = AvaloniaProperty
		.Register<DocumentEditorView, TextDocument?>(name: nameof(Document));

	/// <summary>
	/// Identifies the <see cref="EncodingName" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> EncodingNameProperty = AvaloniaProperty
		.Register<DocumentEditorView, string?>(name: nameof(EncodingName));

	/// <summary>
	/// Identifies the <see cref="Encoding" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> EncodingProperty = AvaloniaProperty
		.Register<DocumentEditorView, string?>(name: nameof(Encoding));

	/// <summary>
	/// Identifies the <see cref="FindUnreadableEncodingsCommand" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<ICommand?> FindUnreadableEncodingsCommandProperty = AvaloniaProperty
		.Register<DocumentEditorView, ICommand?>(name: nameof(FindUnreadableEncodingsCommand));

	/// <summary>
	/// Identifies the <see cref="IsReadOnly" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty
		.Register<DocumentEditorView, bool>(name: nameof(IsReadOnly));

	/// <summary>
	/// Identifies the <see cref="IsSensitive" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsSensitiveProperty = AvaloniaProperty
		.Register<DocumentEditorView, bool>(name: nameof(IsSensitive));

	/// <summary>
	/// Identifies the <see cref="IsSplit" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsSplitProperty = AvaloniaProperty
		.Register<DocumentEditorView, bool>(name: nameof(IsSplit));

	/// <summary>
	/// Identifies the <see cref="ShowEndOfLine" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> ShowEndOfLineProperty = AvaloniaProperty
		.Register<DocumentEditorView, bool>(name: nameof(ShowEndOfLine));

	/// <summary>
	/// Identifies the <see cref="ShowSpaces" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> ShowSpacesProperty = AvaloniaProperty
		.Register<DocumentEditorView, bool>(name: nameof(ShowSpaces));

	/// <summary>
	/// Identifies the <see cref="ShowTabs" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> ShowTabsProperty = AvaloniaProperty
		.Register<DocumentEditorView, bool>(name: nameof(ShowTabs));

	/// <summary>
	/// Identifies the <see cref="SplitShare" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<double> SplitShareProperty = AvaloniaProperty
		.Register<DocumentEditorView, double>(
			name: nameof(SplitShare),
			defaultValue: 0.5);

	/// <summary>
	/// Identifies the <see cref="SyntaxLanguage" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> SyntaxLanguageProperty = AvaloniaProperty
		.Register<DocumentEditorView, string?>(name: nameof(SyntaxLanguage));

	/// <summary>
	/// Identifies the <see cref="ToolBarContent" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<object?> ToolBarContentProperty = AvaloniaProperty
		.Register<DocumentEditorView, object?>(name: nameof(ToolBarContent));

	/// <summary>
	/// Identifies the <see cref="UnreadableEncodings" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<IReadOnlyCollection<string>?> UnreadableEncodingsProperty = AvaloniaProperty
		.Register<DocumentEditorView, IReadOnlyCollection<string>?>(name: nameof(UnreadableEncodings));

	/// <summary>
	/// Identifies the <see cref="ViewState" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<DocumentViewState?> ViewStateProperty = AvaloniaProperty
		.Register<DocumentEditorView, DocumentViewState?>(name: nameof(ViewState));

	/// <summary>
	/// Identifies the <see cref="WordWrap" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> WordWrapProperty = AvaloniaProperty
		.Register<DocumentEditorView, bool>(name: nameof(WordWrap));
	#endregion

	#region Data
	/// <summary>
	/// <c>True</c> once the control has been loaded.
	/// </summary>
	private bool _hasBeenLoaded;
	#endregion

	#region Constructors
	public DocumentEditorView()
	{
		// Before the markup, whose bindings read it once.
		ConvertLineEndingsCommand = new(ConvertLineEndings, CanConvertLineEndings);

		InitializeComponent();
	}
	#endregion

	#region Methods
	/// <summary>
	/// Removes the syntax highlighting of the editor, whose tokenizers keep it in memory.
	/// </summary>
	public void Dispose() => Editor.Dispose();

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);

		// Shown again, as by a switch of tabs; posted, so it comes after the tabs move the focus to their header.
		if (_hasBeenLoaded)
		{
			Dispatcher
				.UIThread
				.Post(FocusText, DispatcherPriority.Loaded);

			return;
		}

		_hasBeenLoaded = true;

		DispatcherTimer.RunOnce(() => Editor.ActiveEditor.Focus(), TimeSpan.FromMilliseconds(100));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the choice of a line break style, named as the status bar names it.
	/// </summary>
	private static SelectorChoice CreateLineEndingChoice(LineEnding lineEnding)
	{
		return new()
		{
			Id = lineEnding.ToString(),
			Name = lineEnding.ToCaption()!,
			SearchTerms = []
		};
	}

	/// <summary>
	/// Validates <see cref="ConvertLineEndingsCommand" />.
	/// </summary>
	private bool CanConvertLineEndings(string? id)
	{
		return Enum.TryParse(id, out LineEnding lineEnding) && Editor
			.ActiveEditor
			.ConvertLineEndingsCommand
			.CanExecute(lineEnding);
	}

	/// <summary>
	/// Brings every line break of the document to a style, through the active half.
	/// </summary>
	private void ConvertLineEndings(string? id)
	{
		if (!Enum.TryParse(id, out LineEnding lineEnding))
		{
			return;
		}

		Editor
			.ActiveEditor
			.ConvertLineEndingsCommand
			.Execute(lineEnding);
	}

	/// <summary>
	/// Moves the keyboard focus into the text of the active half while the focus is nowhere or in the tabs that show
	/// the control.
	/// </summary>
	private void FocusText()
	{
		// The focus elsewhere in the window, such as in a tree a tab was closed from, stays where it is.
		if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is not null
			&& this.FindAncestorOfType<TabControl>() is not { IsKeyboardFocusWithin: true })
		{
			return;
		}

		// The text area itself: the editor would pass the focus on from its own handler, which leaves the input method
		// on the editor.
		Editor
			.ActiveEditor
			.TextArea
			.Focus();
	}
	#endregion
}
