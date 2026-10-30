using Avalonia;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Controls.Text;
using DataOrganizer.Extensions.Views;
using DataOrganizer.Messages.Documents;
using EditorDocuments.Dto;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace DataOrganizer.Behaviors.Text;

/// <summary>
/// Keeps the view state of the document in the associated <see cref="SplitDocumentEditor" />: reports the caret, the
/// selection, the scroll position, the bookmarks and the folded blocks of the active half through <see cref="ViewState" />,
/// and brings back a state set from outside once the document has been laid out.
/// </summary>
internal sealed class DocumentViewStateBehavior :
	StyledElementBehavior<SplitDocumentEditor>,
	IRecipient<BookmarksChangedMessage>,
	IRecipient<FoldingChangedMessage>
{
	#region Properties
	/// <summary>
	/// Caret, selection, scroll position, bookmarks and folded blocks of the document.
	/// A value set from outside is restored once the document has been laid out.
	/// </summary>
	public DocumentViewState? ViewState
	{
		get => GetValue(ViewStateProperty);
		set => SetValue(ViewStateProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="ViewState" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<DocumentViewState?> ViewStateProperty = AvaloniaProperty
		.Register<DocumentViewStateBehavior, DocumentViewState?>(name: nameof(ViewState));
	#endregion

	#region Data
	/// <summary>
	/// Subscriptions to the halves of the editor while the behavior is attached.
	/// </summary>
	private readonly SerialDisposable _editorSubscriptions = new();

	/// <summary>
	/// <c>True</c> while the behavior writes <see cref="ViewState" /> itself, so the change is not restored back.
	/// </summary>
	private bool _isCapturing;

	/// <summary>
	/// View state set from outside and not restored yet.
	/// </summary>
	private DocumentViewState? _pendingViewState;
	#endregion

	#region Constructors
	public DocumentViewStateBehavior()
	{
		this
			.GetObservable(ViewStateProperty)
			.Subscribe(ViewStateProperty_Changed);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="SplitDocumentEditor.ActiveEditorProperty" /> changed handler of
	/// <see cref="StyledElementBehavior{T}.AssociatedObject" />.
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

		if (AssociatedObject is not { IsLoaded: true })
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
		if (AssociatedObject is not { } editor || message.Bookmarks != editor.ActiveEditor.Bookmarks)
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
		if (AssociatedObject is not { } editor || message.Editor != editor.ActiveEditor)
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
		// A capture before the restore would overwrite the state that is still to be shown.
		if (_pendingViewState is not null
			|| AssociatedObject?.ActiveEditor is not { } editor
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
			CaretPosition = ToCaretPosition(editor.TextArea.Caret.Position),
			FoldedBlocks = isMostlyFolded ? null : foldedBlocks,
			ScrollOffset = new()
			{
				X = scrollViewer.Offset.X,
				Y = scrollViewer.Offset.Y
			},
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
	protected override void OnAttached()
	{
		base.OnAttached();

		if (AssociatedObject is not { } editor)
		{
			return;
		}

		// The state is that of the active half, so only its changes count.
		_editorSubscriptions.Disposable = new CompositeDisposable(
			editor
				.GetObservable(SplitDocumentEditor.ActiveEditorProperty)
				.Select(BuildChangeTrigger)
				.Switch()
				.SetDelay(TimeSpan.FromSeconds(0.5))
				.Subscribe(_ => CaptureViewState()),
			editor
				.GetObservable(SplitDocumentEditor.ActiveEditorProperty)
				.Subscribe(ActiveEditorProperty_Changed));
	}

	/// <inheritdoc />
	protected override void OnAttachedToVisualTree()
	{
		base.OnAttachedToVisualTree();

		// Bookmarks and folds change while neither the caret nor the view moves, whose moves the state follows otherwise.
		WeakReferenceMessenger
			.Default
			.RegisterAll(this);
	}

	/// <inheritdoc />
	protected override void OnDetachedFromVisualTree()
	{
		WeakReferenceMessenger
			.Default
			.UnregisterAll(this);

		base.OnDetachedFromVisualTree();
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		_editorSubscriptions.Disposable = null;

		base.OnDetaching();
	}

	/// <inheritdoc />
	protected override void OnLoaded()
	{
		base.OnLoaded();

		if (_pendingViewState is null)
		{
			return;
		}

		ScheduleRestore();
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
	/// Returns the caret of the view state for the one of the editor.
	/// </summary>
	private static CaretPosition ToCaretPosition(TextViewPosition position)
	{
		return new()
		{
			Column = position.Column,
			IsAtEndOfLine = position.IsAtEndOfLine,
			Line = position.Line,
			VisualColumn = position.VisualColumn
		};
	}

	/// <summary>
	/// Returns the caret of the editor for the one of the view state.
	/// </summary>
	private static TextViewPosition ToTextViewPosition(CaretPosition position)
	{
		return new(
			line: position.Line,
			column: position.Column,
			visualColumn: position.VisualColumn)
		{
			IsAtEndOfLine = position.IsAtEndOfLine
		};
	}

	/// <summary>
	/// Applies the pending view state to the laid out document.
	/// </summary>
	private void RestoreViewState()
	{
		// The upper half takes the state, and a lower half, when there is one, follows it.
		if (_pendingViewState is not { } state
			|| AssociatedObject is not { } splitEditor
			|| splitEditor.PrimaryEditor is not { ScrollViewer: not null, Document: { } document } editor)
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

		// The state keeps the shorter list of blocks, the unfolded or the folded ones, and the offset, not the caret line,
		// brings the view back the way it was left.
		editor.SetView(
			selectionStart: start,
			selectionLength: length,
			caretPosition: ToTextViewPosition(state.CaretPosition),
			blockStarts: state.UnfoldedBlocks ?? state.FoldedBlocks ?? [],
			isFolded: state.UnfoldedBlocks is null,
			scrollOffset: new(state.ScrollOffset.X, state.ScrollOffset.Y));

		editor
			.Bookmarks
			.SetLines(state.Bookmarks ?? []);

		// A split that came before the document opens the lower half at the restored place only now.
		splitEditor.CopyViewToSecondaryEditor();
	}

	/// <summary>
	/// Restores the pending view state after the layout pass, so that a document set along with it
	/// is already in place and does not reset the caret and the scroll position afterwards.
	/// </summary>
	private void ScheduleRestore() => Dispatcher.UIThread.Post(RestoreViewState, DispatcherPriority.Loaded);
	#endregion
}
