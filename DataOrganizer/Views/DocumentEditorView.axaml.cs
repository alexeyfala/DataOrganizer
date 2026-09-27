using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Controls;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Extensions;
using DataOrganizer.Messages.Documents;
using System;
using System.Reactive.Linq;

namespace DataOrganizer.Views;

/// <summary>
/// Text editor of a <see cref="TextDocument" /> with its toolbar, context menu, status bar and zoom.
/// </summary>
internal sealed partial class DocumentEditorView : UserControl, IRecipient<BookmarksChangedMessage>
{
	#region Properties
	/// <summary>
	/// Editor of the half focused last.
	/// </summary>
	public DocumentTextEditor ActiveEditor
	{
		get => _activeEditor;
		private set => SetAndRaise(ActiveEditorProperty, ref _activeEditor, value);
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
	/// Name of the encoding the document is stored in; <c>null</c> for a document that is not stored as bytes.
	/// </summary>
	public string? EncodingName
	{
		get => GetValue(EncodingNameProperty);
		set => SetValue(EncodingNameProperty, value);
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
	/// Content placed at the end of the toolbar.
	/// </summary>
	public object? ToolBarContent
	{
		get => GetValue(ToolBarContentProperty);
		set => SetValue(ToolBarContentProperty, value);
	}

	/// <summary>
	/// Caret, selection, scroll position and bookmarks of the document.
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

	#region Styled Properties
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
	/// Identifies the <see cref="ToolBarContent" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<object?> ToolBarContentProperty = AvaloniaProperty
		.Register<DocumentEditorView, object?>(name: nameof(ToolBarContent));

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

	#region Direct Properties
	/// <summary>
	/// Identifies the <see cref="ActiveEditor" /> avalonia property.
	/// </summary>
	public static readonly DirectProperty<DocumentEditorView, DocumentTextEditor> ActiveEditorProperty = AvaloniaProperty
		.RegisterDirect<DocumentEditorView, DocumentTextEditor>(
			name: nameof(ActiveEditor),
			getter: static x => x.ActiveEditor);
	#endregion

	#region Data
	/// <summary>
	/// Name of the scroll viewer in the template of <see cref="TextEditor" />.
	/// </summary>
	private const string ScrollViewerPartName = "PART_ScrollViewer";

	/// <inheritdoc cref="ActiveEditor" />
	private DocumentTextEditor _activeEditor = null!;

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

	/// <summary>
	/// Scroll viewer of <see cref="Editor" />.
	/// </summary>
	private ScrollViewer? _scrollViewer;

	/// <summary>
	/// Scroll viewer of <see cref="SplitEditor" />.
	/// </summary>
	private ScrollViewer? _splitScrollViewer;
	#endregion

	#region Constructors
	public DocumentEditorView()
	{
		InitializeComponent();

		// The markup binds to the active half before the halves are built, so the change has to reach it.
		ActiveEditor = Editor;

		// The editors never outlive this control, so none of the handlers below is ever removed.
		Editor.TemplateApplied += Editor_TemplateApplied;

		SplitEditor.TemplateApplied += SplitEditor_TemplateApplied;

		// The editor marks every focus event within it handled.
		Editor.AddHandler(
			GotFocusEvent,
			DocumentTextEditor_GotFocus,
			RoutingStrategies.Bubble,
			handledEventsToo: true);

		SplitEditor.AddHandler(
			GotFocusEvent,
			DocumentTextEditor_GotFocus,
			RoutingStrategies.Bubble,
			handledEventsToo: true);

		// On the tunnel a right click reaches its half before the context menu opens.
		Editor.AddHandler(
			PointerPressedEvent,
			DocumentTextEditor_PointerPressed,
			RoutingStrategies.Tunnel);

		SplitEditor.AddHandler(
			PointerPressedEvent,
			DocumentTextEditor_PointerPressed,
			RoutingStrategies.Tunnel);

		TextArea area = Editor.TextArea;

		TextArea splitArea = SplitEditor.TextArea;

		// The state is that of the active half, so the moves of either half count.
		Observable.FromEventPattern<EventHandler, EventArgs>(
			x => area.Caret.PositionChanged += x,
			x => area.Caret.PositionChanged -= x)
			.Merge(Observable.FromEventPattern<EventHandler, EventArgs>(
				x => area.TextView.ScrollOffsetChanged += x,
				x => area.TextView.ScrollOffsetChanged -= x))
			.Merge(Observable.FromEventPattern<EventHandler, EventArgs>(
				x => splitArea.Caret.PositionChanged += x,
				x => splitArea.Caret.PositionChanged -= x))
			.Merge(Observable.FromEventPattern<EventHandler, EventArgs>(
				x => splitArea.TextView.ScrollOffsetChanged += x,
				x => splitArea.TextView.ScrollOffsetChanged -= x))
			.SetDelay(TimeSpan.FromSeconds(0.5))
			.Subscribe(_ => CaptureViewState());

		this
			.GetObservable(ViewStateProperty)
			.Subscribe(ViewStateProperty_Changed);

		this
			.GetObservable(IsSplitProperty)
			.Subscribe(IsSplitProperty_Changed);

		this
			.GetObservable(ActiveEditorProperty)
			.Subscribe(ActiveEditorProperty_Changed);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="ActiveEditorProperty" /> changed handler.
	/// </summary>
	private void ActiveEditorProperty_Changed(DocumentTextEditor value)
	{
		// The state is that of the active half, which a move of the focus changes without a move of the caret or the view.
		CaptureViewState();
	}

	/// <summary>
	/// <see cref="InputElement.GotFocus" /> event handler of both halves.
	/// </summary>
	private void DocumentTextEditor_GotFocus(object? sender, FocusChangedEventArgs e)
	{
		if (sender is not DocumentTextEditor editor)
		{
			return;
		}

		ActiveEditor = editor;
	}

	/// <summary>
	/// <see cref="InputElement.PointerPressedEvent" /> handler of both halves, which gives a half the focus on a right click too.
	/// </summary>
	private void DocumentTextEditor_PointerPressed(object? sender, PointerPressedEventArgs e)
	{
		// The text area takes the focus on any press, but beside it, on the scroll bar or the scroll marks,
		// only the left button moves the focus, while the context menu serves the active half.
		if (sender is not DocumentTextEditor { IsKeyboardFocusWithin: false } editor
			|| !e.GetCurrentPoint(editor).Properties.IsRightButtonPressed)
		{
			return;
		}

		editor
			.TextArea
			.Focus();
	}

	/// <summary>
	/// <see cref="TemplatedControl.TemplateApplied" /> event handler of <see cref="Editor" />.
	/// </summary>
	private void Editor_TemplateApplied(object? sender, TemplateAppliedEventArgs e)
	{
		_scrollViewer = e.NameScope.Find<ScrollViewer>(ScrollViewerPartName);
	}

	/// <summary>
	/// <see cref="IsSplitProperty" /> changed handler.
	/// </summary>
	private void IsSplitProperty_Changed(bool value)
	{
		RowDefinitions rows = EditorsHost.RowDefinitions;

		// A drag of the splitter leaves its own shares in both rows, so every split starts in the middle.
		rows[Grid.GetRow(Editor)].Height = GridLength.Star;

		// The row of the hidden lower half shrinks to nothing and leaves the whole height to the upper one.
		rows[Grid.GetRow(SplitEditor)].Height = value ? GridLength.Star : GridLength.Auto;

		if (!value)
		{
			// The upper half stays, so it takes over the place of an active lower half.
			if (ActiveEditor == SplitEditor)
			{
				CopyViewToEditor();
			}

			ActiveEditor = Editor;

			return;
		}

		// The lower half gets its layout only once it is shown.
		Dispatcher.UIThread.Post(CopyViewToSplitEditor, DispatcherPriority.Loaded);
	}

	/// <summary>
	/// <see cref="TemplatedControl.TemplateApplied" /> event handler of <see cref="SplitEditor" />.
	/// </summary>
	private void SplitEditor_TemplateApplied(object? sender, TemplateAppliedEventArgs e)
	{
		_splitScrollViewer = e
			.NameScope
			.Find<ScrollViewer>(ScrollViewerPartName);
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
	/// Reports the view state again when the bookmarks of the document change.
	/// </summary>
	public void Receive(BookmarksChangedMessage message)
	{
		// Every open document has bookmarks of its own, which both halves share.
		if (message.Bookmarks != Editor.Bookmarks)
		{
			return;
		}

		CaptureViewState();
	}

	/// <summary>
	/// Reports the caret, selection, scroll position and bookmarks through <see cref="ViewState" />.
	/// </summary>
	internal void CaptureViewState()
	{
		DocumentTextEditor editor = ActiveEditor;

		// A capture before the restore would overwrite the state that is still to be shown.
		if (_pendingViewState is not null
			|| GetScrollViewer(editor) is not { } scrollViewer
			|| editor.Document is null)
		{
			return;
		}

		DocumentViewState state = new()
		{
			Bookmarks = editor.Bookmarks.GetLines(),
			CaretPosition = editor.TextArea.Caret.Position,
			ScrollOffset = scrollViewer.Offset,
			SelectionLength = editor.SelectionLength,
			SelectionStart = editor.SelectionStart
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

		// A click on the bookmark margin moves neither the caret nor the view, whose moves the state follows otherwise.
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

		DispatcherTimer.RunOnce(() => Editor.Focus(), TimeSpan.FromMilliseconds(100));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Puts the caret, the selection and the scroll position of one editor into another one on the same document.
	/// </summary>
	private static void CopyView(
		TextEditor source,
		ScrollViewer sourceScrollViewer,
		TextEditor target,
		ScrollViewer targetScrollViewer)
	{
		target.Select(source.SelectionStart, source.SelectionLength);

		target
			.TextArea
			.Caret
			.Position = source.TextArea.Caret.Position;

		targetScrollViewer.Offset = sourceScrollViewer.Offset;
	}

	/// <summary>
	/// Puts the caret, the selection and the scroll position of the lower half into the upper one.
	/// </summary>
	private void CopyViewToEditor()
	{
		if (_scrollViewer is not { } scrollViewer
			|| _splitScrollViewer is not { } splitScrollViewer)
		{
			return;
		}

		CopyView(
			SplitEditor,
			splitScrollViewer,
			Editor,
			scrollViewer);
	}

	/// <summary>
	/// Opens the lower half at the caret, the selection and the scroll position of the upper one.
	/// </summary>
	private void CopyViewToSplitEditor()
	{
		if (!IsSplit
			|| _scrollViewer is not { } scrollViewer
			|| _splitScrollViewer is not { } splitScrollViewer)
		{
			return;
		}

		CopyView(
			Editor,
			scrollViewer,
			SplitEditor,
			splitScrollViewer);
	}

	/// <summary>
	/// Returns the scroll viewer of a half.
	/// </summary>
	private ScrollViewer? GetScrollViewer(DocumentTextEditor editor) => editor == SplitEditor ? _splitScrollViewer : _scrollViewer;

	/// <summary>
	/// Applies the pending view state to the laid out document.
	/// </summary>
	private void RestoreViewState()
	{
		if (_pendingViewState is not { } state
			|| _scrollViewer is not { } scrollViewer
			|| Editor.Document is not { } document)
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

		Editor.Select(start, length);

		Editor
			.TextArea
			.Caret
			.Position = state.CaretPosition;

		// The offset, not the caret line, brings the view back the way it was left.
		scrollViewer.Offset = state.ScrollOffset;

		Editor
			.Bookmarks
			.SetLines(state.Bookmarks ?? []);
	}

	/// <summary>
	/// Restores the pending view state after the layout pass, so that a document set along with it
	/// is already in place and does not reset the caret and the scroll position afterwards.
	/// </summary>
	private void ScheduleRestore() => Dispatcher.UIThread.Post(RestoreViewState, DispatcherPriority.Loaded);
	#endregion
}
