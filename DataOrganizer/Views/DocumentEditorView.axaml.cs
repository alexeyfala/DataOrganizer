using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Controls;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Messages.Documents;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;

namespace DataOrganizer.Views;

/// <summary>
/// Text editor of a <see cref="TextDocument" /> with its toolbar, context menu, status bar and zoom.
/// </summary>
internal sealed partial class DocumentEditorView :
	UserControl,
	IDisposable,
	IRecipient<BookmarksChangedMessage>,
	IRecipient<FoldingChangedMessage>
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

	/// <summary>
	/// <c>True</c> while the control writes <see cref="ViewState" /> itself, so the change is not restored back.
	/// </summary>
	private bool _isCapturing;

	/// <summary>
	/// View state set from outside and not restored yet.
	/// </summary>
	private DocumentViewState? _pendingViewState;
	#endregion

	#region Constructors
	public DocumentEditorView()
	{
		// Before the markup, whose bindings read it once.
		ConvertLineEndingsCommand = new(ConvertLineEndings, CanConvertLineEndings);

		InitializeComponent();

		// The state is that of the active half, so only its changes count.
		Editor
			.GetObservable(SplitDocumentEditor.ActiveEditorProperty)
			.Select(BuildChangeTrigger)
			.Switch()
			.SetDelay(TimeSpan.FromSeconds(0.5))
			.Subscribe(_ => CaptureViewState());

		this
			.GetObservable(ViewStateProperty)
			.Subscribe(ViewStateProperty_Changed);

		Editor
			.GetObservable(SplitDocumentEditor.ActiveEditorProperty)
			.Subscribe(ActiveEditorProperty_Changed);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="SplitDocumentEditor.ActiveEditorProperty" /> changed handler of <see cref="Editor" />.
	/// </summary>
	private void ActiveEditorProperty_Changed(DocumentTextEditor value)
	{
		// The state is that of the active half, which a move of the focus changes without a move of the caret or the view.
		CaptureViewState();
	}

	/// <summary>
	/// <see cref="ViewStateProperty" /> changed handler.
	/// </summary>
	private void ViewStateProperty_Changed(DocumentViewState? value)
	{
		if (_isCapturing || value is not { } state)
		{
			return;
		}

		_pendingViewState = state;

		if (!IsLoaded)
		{
			return;
		}

		ScheduleRestore();
	}
	#endregion

	#region Methods
	/// <summary>
	/// Removes the syntax highlighting of the editor, whose tokenizers keep it in memory.
	/// </summary>
	public void Dispose() => Editor.Dispose();

	/// <summary>
	/// Reports the view state again when the bookmarks of the document change.
	/// </summary>
	public void Receive(BookmarksChangedMessage message)
	{
		// Every open document has bookmarks of its own, which both halves share.
		if (message.Bookmarks != Editor.ActiveEditor.Bookmarks)
		{
			return;
		}

		CaptureViewState();
	}

	/// <summary>
	/// Reports the view state again when blocks of the active half fold or unfold.
	/// </summary>
	public void Receive(FoldingChangedMessage message)
	{
		// Each half folds on its own, and the state is that of the active one.
		if (message.Editor != Editor.ActiveEditor)
		{
			return;
		}

		CaptureViewState();
	}

	/// <summary>
	/// Reports the caret, selection, scroll position, bookmarks and folded blocks through <see cref="ViewState" />.
	/// </summary>
	internal void CaptureViewState()
	{
		DocumentTextEditor editor = Editor.ActiveEditor;

		// A capture before the restore would overwrite the state that is still to be shown.
		if (_pendingViewState is not null
			|| editor.ScrollViewer is not { } scrollViewer
			|| editor.Document is null)
		{
			return;
		}

		int[] foldedBlocks = editor.GetBlockStarts(isFolded: true);

		int[] unfoldedBlocks = editor.GetBlockStarts(isFolded: false);

		// The shorter list is kept, so a text with every block folded costs as little as one with none.
		bool isMostlyFolded = unfoldedBlocks.Length < foldedBlocks.Length;

		DocumentViewState state = new()
		{
			Bookmarks = editor.Bookmarks.GetLines(),
			CaretPosition = editor.TextArea.Caret.Position,
			FoldedBlocks = isMostlyFolded ? null : foldedBlocks,
			ScrollOffset = scrollViewer.Offset,
			SelectionLength = editor.SelectionLength,
			SelectionStart = editor.SelectionStart,
			UnfoldedBlocks = isMostlyFolded ? unfoldedBlocks : null
		};

		_isCapturing = true;

		try
		{
			SetCurrentValue(ViewStateProperty, state);
		}
		finally
		{
			_isCapturing = false;
		}
	}

	/// <inheritdoc />
	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);

		// Bookmarks and folds change while neither the caret nor the view moves, whose moves the state follows otherwise.
		WeakReferenceMessenger
			.Default
			.RegisterAll(this);
	}

	/// <inheritdoc />
	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		WeakReferenceMessenger
			.Default
			.UnregisterAll(this);

		base.OnDetachedFromVisualTree(e);
	}

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);

		if (_pendingViewState is not null)
		{
			ScheduleRestore();
		}

		if (_hasBeenLoaded)
		{
			return;
		}

		_hasBeenLoaded = true;

		DispatcherTimer.RunOnce(() => Editor.ActiveEditor.Focus(), TimeSpan.FromMilliseconds(100));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Emits when the caret or the view of an editor moves, or when the view builds its lines anew, as a click that
	/// folds a block makes it do.
	/// </summary>
	private static IObservable<EventPattern<EventArgs>> BuildChangeTrigger(TextEditor editor)
	{
		TextArea area = editor.TextArea;

		return Observable.FromEventPattern<EventHandler, EventArgs>(
			x => area.Caret.PositionChanged += x,
			x => area.Caret.PositionChanged -= x)
			.Merge(Observable.FromEventPattern<EventHandler, EventArgs>(
				x => area.TextView.ScrollOffsetChanged += x,
				x => area.TextView.ScrollOffsetChanged -= x))
			.Merge(Observable.FromEventPattern<EventHandler, EventArgs>(
				x => area.TextView.VisualLinesChanged += x,
				x => area.TextView.VisualLinesChanged -= x));
	}

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
	/// Applies the pending view state to the laid out document.
	/// </summary>
	private void RestoreViewState()
	{
		// The upper half takes the state, and a lower half, when there is one, follows it.
		DocumentTextEditor editor = Editor.PrimaryEditor;

		if (_pendingViewState is not { } state
			|| editor.ScrollViewer is not { } scrollViewer
			|| editor.Document is not { } document)
		{
			return;
		}

		_pendingViewState = null;

		int start = Math.Clamp(
			state.SelectionStart,
			0,
			document.TextLength);

		int length = Math.Clamp(
			state.SelectionLength,
			0,
			document.TextLength - start);

		editor.Select(start, length);

		editor
			.TextArea
			.Caret
			.Position = state.CaretPosition;

		// After the caret, whose move unfolds a block it lands in, and before the offset, which is measured over the
		// folded text.
		if (state.UnfoldedBlocks is { } unfoldedBlocks)
		{
			editor.SetBlocksFolded(unfoldedBlocks, isFolded: false);
		}
		else
		{
			editor.SetBlocksFolded(state.FoldedBlocks ?? [], isFolded: true);
		}

		// The offset, not the caret line, brings the view back the way it was left.
		scrollViewer.Offset = state.ScrollOffset;

		editor
			.Bookmarks
			.SetLines(state.Bookmarks ?? []);

		// A split that came before the document opens the lower half at the restored place only now.
		Editor.CopyViewToSecondaryEditor();
	}

	/// <summary>
	/// Restores the pending view state after the layout pass, so that a document set along with it
	/// is already in place and does not reset the caret and the scroll position afterwards.
	/// </summary>
	private void ScheduleRestore() => Dispatcher.UIThread.Post(RestoreViewState, DispatcherPriority.Loaded);
	#endregion
}
