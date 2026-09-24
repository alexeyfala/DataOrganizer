using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using DataOrganizer.Dto;
using DataOrganizer.Extensions;
using System;
using System.Reactive.Linq;

namespace DataOrganizer.Views;

/// <summary>
/// Text editor of a <see cref="TextDocument" /> with its toolbar, context menu and zoom.
/// </summary>
internal sealed partial class DocumentEditor : UserControl
{
	#region Properties
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
	/// <c>True</c> when the document cannot be edited.
	/// </summary>
	public bool IsReadOnly
	{
		get => GetValue(IsReadOnlyProperty);
		set => SetValue(IsReadOnlyProperty, value);
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
	/// Caret, selection and scroll position of the document.
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
		.Register<DocumentEditor, double>(
			name: nameof(DocumentFontSize),
			defaultValue: 14.0);

	/// <summary>
	/// Identifies the <see cref="Document" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<TextDocument?> DocumentProperty = AvaloniaProperty
		.Register<DocumentEditor, TextDocument?>(name: nameof(Document));

	/// <summary>
	/// Identifies the <see cref="IsReadOnly" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty
		.Register<DocumentEditor, bool>(name: nameof(IsReadOnly));

	/// <summary>
	/// Identifies the <see cref="ShowEndOfLine" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> ShowEndOfLineProperty = AvaloniaProperty
		.Register<DocumentEditor, bool>(name: nameof(ShowEndOfLine));

	/// <summary>
	/// Identifies the <see cref="ShowSpaces" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> ShowSpacesProperty = AvaloniaProperty
		.Register<DocumentEditor, bool>(name: nameof(ShowSpaces));

	/// <summary>
	/// Identifies the <see cref="ShowTabs" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> ShowTabsProperty = AvaloniaProperty
		.Register<DocumentEditor, bool>(name: nameof(ShowTabs));

	/// <summary>
	/// Identifies the <see cref="ToolBarContent" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<object?> ToolBarContentProperty = AvaloniaProperty
		.Register<DocumentEditor, object?>(name: nameof(ToolBarContent));

	/// <summary>
	/// Identifies the <see cref="ViewState" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<DocumentViewState?> ViewStateProperty = AvaloniaProperty
		.Register<DocumentEditor, DocumentViewState?>(name: nameof(ViewState));

	/// <summary>
	/// Identifies the <see cref="WordWrap" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> WordWrapProperty = AvaloniaProperty
		.Register<DocumentEditor, bool>(name: nameof(WordWrap));
	#endregion

	#region Data
	/// <summary>
	/// Name of the scroll viewer in the template of <see cref="TextEditor" />.
	/// </summary>
	private const string ScrollViewerPartName = "PART_ScrollViewer";

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
	/// Scroll viewer of <see cref="TextEditor" />.
	/// </summary>
	private ScrollViewer? _scrollViewer;
	#endregion

	#region Constructors
	public DocumentEditor()
	{
		InitializeComponent();

		// The editor never outlives this control, so none of the handlers below is ever removed.
		Editor.TemplateApplied += Editor_TemplateApplied;

		TextArea area = Editor.TextArea;

		Observable.FromEventPattern<EventHandler, EventArgs>(
			x => area.Caret.PositionChanged += x,
			x => area.Caret.PositionChanged -= x)
			.Merge(Observable.FromEventPattern<EventHandler, EventArgs>(
				x => area.TextView.ScrollOffsetChanged += x,
				x => area.TextView.ScrollOffsetChanged -= x))
			.SetDelay(TimeSpan.FromSeconds(0.5))
			.Subscribe(_ => CaptureViewState());

		this
			.GetObservable(ViewStateProperty)
			.Subscribe(ViewStateProperty_Changed);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="TemplatedControl.TemplateApplied" /> event handler of <see cref="TextEditor" />.
	/// </summary>
	private void Editor_TemplateApplied(object? sender, TemplateAppliedEventArgs e)
	{
		_scrollViewer = e.NameScope.Find<ScrollViewer>(ScrollViewerPartName);
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
	/// Reports the caret, selection and scroll position through <see cref="ViewState" />.
	/// </summary>
	internal void CaptureViewState()
	{
		// A capture before the restore would overwrite the state that is still to be shown.
		if (_pendingViewState is not null
			|| _scrollViewer is not { } scrollViewer
			|| Editor.Document is null)
		{
			return;
		}

		DocumentViewState state = new()
		{
			CaretPosition = Editor.TextArea.Caret.Position,
			ScrollOffset = scrollViewer.Offset,
			SelectionLength = Editor.SelectionLength,
			SelectionStart = Editor.SelectionStart
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

		int start = Math.Clamp(state.SelectionStart, 0, document.TextLength);

		int length = Math.Clamp(state.SelectionLength, 0, document.TextLength - start);

		Editor.Select(start, length);

		Editor
			.TextArea
			.Caret
			.Position = state.CaretPosition;

		// The offset, not the caret line, brings the view back the way it was left.
		scrollViewer.Offset = state.ScrollOffset;
	}

	/// <summary>
	/// Restores the pending view state after the layout pass, so that a document set along with it
	/// is already in place and does not reset the caret and the scroll position afterwards.
	/// </summary>
	private void ScheduleRestore() => Dispatcher.UIThread.Post(RestoreViewState, DispatcherPriority.Loaded);
	#endregion
}
