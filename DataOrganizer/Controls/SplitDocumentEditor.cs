using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using DataOrganizer.Behaviors.Styling;
using System;
using System.Reactive.Linq;

namespace DataOrganizer.Controls;

/// <summary>
/// Editor of a <see cref="TextDocument" /> that can be split into two halves one above the other,
/// like the code window of Visual Studio.
/// </summary>
internal sealed class SplitDocumentEditor : Control
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
	/// The document shown in both halves.
	/// </summary>
	public TextDocument? Document
	{
		get => GetValue(DocumentProperty);
		set => SetValue(DocumentProperty, value);
	}

	/// <summary>
	/// Font size of the text, which a zoom in either half changes for both.
	/// </summary>
	public double FontSize
	{
		get => GetValue(FontSizeProperty);
		set => SetValue(FontSizeProperty, value);
	}

	/// <summary>
	/// Brush of the text, which both halves inherit.
	/// </summary>
	public IBrush? Foreground
	{
		get => GetValue(ForegroundProperty);
		set => SetValue(ForegroundProperty, value);
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
	/// Editor of the upper half, which is always shown.
	/// </summary>
	public DocumentTextEditor PrimaryEditor { get; }

	/// <summary>
	/// Editor of the lower half; <c>null</c> until the first split.
	/// </summary>
	public DocumentTextEditor? SecondaryEditor { get; private set; }

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
	/// Identifies the <see cref="Document" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<TextDocument?> DocumentProperty = AvaloniaProperty
		.Register<SplitDocumentEditor, TextDocument?>(name: nameof(Document));

	/// <summary>
	/// Identifies the <see cref="FontSize" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<double> FontSizeProperty = TextElement
		.FontSizeProperty
		.AddOwner<SplitDocumentEditor>();

	/// <summary>
	/// Identifies the <see cref="Foreground" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<IBrush?> ForegroundProperty = TextElement
		.ForegroundProperty
		.AddOwner<SplitDocumentEditor>();

	/// <summary>
	/// Identifies the <see cref="IsReadOnly" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty
		.Register<SplitDocumentEditor, bool>(name: nameof(IsReadOnly));

	/// <summary>
	/// Identifies the <see cref="IsSensitive" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsSensitiveProperty = AvaloniaProperty
		.Register<SplitDocumentEditor, bool>(name: nameof(IsSensitive));

	/// <summary>
	/// Identifies the <see cref="IsSplit" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsSplitProperty = AvaloniaProperty
		.Register<SplitDocumentEditor, bool>(name: nameof(IsSplit));

	/// <summary>
	/// Identifies the <see cref="ShowEndOfLine" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> ShowEndOfLineProperty = AvaloniaProperty
		.Register<SplitDocumentEditor, bool>(name: nameof(ShowEndOfLine));

	/// <summary>
	/// Identifies the <see cref="ShowSpaces" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> ShowSpacesProperty = AvaloniaProperty
		.Register<SplitDocumentEditor, bool>(name: nameof(ShowSpaces));

	/// <summary>
	/// Identifies the <see cref="ShowTabs" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> ShowTabsProperty = AvaloniaProperty
		.Register<SplitDocumentEditor, bool>(name: nameof(ShowTabs));

	/// <summary>
	/// Identifies the <see cref="WordWrap" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> WordWrapProperty = AvaloniaProperty
		.Register<SplitDocumentEditor, bool>(name: nameof(WordWrap));
	#endregion

	#region Direct Properties
	/// <summary>
	/// Identifies the <see cref="ActiveEditor" /> avalonia property.
	/// </summary>
	public static readonly DirectProperty<SplitDocumentEditor, DocumentTextEditor> ActiveEditorProperty = AvaloniaProperty
		.RegisterDirect<SplitDocumentEditor, DocumentTextEditor>(
			name: nameof(ActiveEditor),
			getter: static x => x.ActiveEditor);
	#endregion

	#region Data
	/// <summary>
	/// The least height of a half in a split view.
	/// </summary>
	private const double MinHalfHeight = 48.0;

	/// <summary>
	/// Name of the editor of the upper half.
	/// </summary>
	private const string PrimaryEditorName = "PrimaryEditor";

	/// <summary>
	/// Row of the upper half.
	/// </summary>
	private const int PrimaryRow = 0;

	/// <summary>
	/// Name of the editor of the lower half.
	/// </summary>
	private const string SecondaryEditorName = "SecondaryEditor";

	/// <summary>
	/// Row of the lower half.
	/// </summary>
	private const int SecondaryRow = 2;

	/// <summary>
	/// Style class of the splitter between the halves.
	/// </summary>
	private const string SplitterClass = "HorizontalGridSplitterStyle";

	/// <summary>
	/// Row of the splitter between the halves.
	/// </summary>
	private const int SplitterRow = 1;

	/// <summary>
	/// Grid of the halves and the splitter between them.
	/// </summary>
	private readonly Grid _grid;

	/// <summary>
	/// Splitter between the halves.
	/// </summary>
	private readonly GridSplitter _splitter;

	/// <inheritdoc cref="ActiveEditor" />
	private DocumentTextEditor _activeEditor;
	#endregion

	#region Constructors
	public SplitDocumentEditor()
	{
		PrimaryEditor = CreateEditor(PrimaryEditorName, PrimaryRow);

		_activeEditor = PrimaryEditor;

		// A drag of the splitter leaves the focus in the text, so the keys still type into it.
		_splitter = new GridSplitter
		{
			Focusable = false
		};

		_splitter.Classes.Add(SplitterClass);

		// The splitter is not part of the text, so the menu of the text stays closed there.
		_splitter.ContextRequested += GridSplitter_ContextRequested;

		Grid.SetRow(_splitter, SplitterRow);

		_grid = new Grid
		{
			RowDefinitions = new RowDefinitions("*,Auto,Auto")
		};

		_grid.Children.Add(PrimaryEditor);

		_grid.Children.Add(_splitter);

		LogicalChildren.Add(_grid);

		VisualChildren.Add(_grid);

		this
			.GetObservable(IsSplitProperty)
			.Subscribe(IsSplitProperty_Changed);
	}
	#endregion

	#region Event Handlers
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
	/// <see cref="Control.ContextRequested" /> event handler of the splitter.
	/// </summary>
	private void GridSplitter_ContextRequested(object? sender, ContextRequestedEventArgs e)
	{
		e.Handled = true;
	}

	/// <summary>
	/// <see cref="IsSplitProperty" /> changed handler.
	/// </summary>
	private void IsSplitProperty_Changed(bool value)
	{
		RowDefinitions rows = _grid.RowDefinitions;

		// A drag of the splitter leaves its own shares in both rows, so every split starts in the middle.
		rows[PrimaryRow].Height = GridLength.Star;

		// The row of the hidden lower half shrinks to nothing and leaves the whole height to the upper one.
		rows[SecondaryRow].Height = value ? GridLength.Star : GridLength.Auto;

		// A drag of the splitter to an edge leaves the half there in view, while a hidden half takes no room.
		double minHeight = value ? MinHalfHeight : 0.0;

		rows[PrimaryRow].MinHeight = minHeight;

		rows[SecondaryRow].MinHeight = minHeight;

		_splitter.IsVisible = value;

		if (!value)
		{
			if (SecondaryEditor is not { } secondaryEditor)
			{
				return;
			}

			// The upper half stays, so it takes over the place of an active lower half.
			if (ActiveEditor == secondaryEditor)
			{
				CopyView(secondaryEditor, PrimaryEditor);
			}

			secondaryEditor.IsVisible = false;

			ActiveEditor = PrimaryEditor;

			return;
		}

		// A document that is never split costs no second editor.
		if (SecondaryEditor is null)
		{
			SecondaryEditor = CreateEditor(SecondaryEditorName, SecondaryRow);

			_grid.Children.Add(SecondaryEditor);
		}

		SecondaryEditor.IsVisible = true;

		// The lower half gets its layout only once it is shown.
		Dispatcher.UIThread.Post(CopyViewToSecondaryEditor, DispatcherPriority.Loaded);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Puts the caret, the selection and the scroll position of one half into the other one.
	/// </summary>
	private static void CopyView(TextEditorBase source, TextEditorBase target)
	{
		if (source.ScrollViewer is not { } sourceScrollViewer
			|| target.ScrollViewer is not { } targetScrollViewer)
		{
			return;
		}

		target.Select(source.SelectionStart, source.SelectionLength);

		target
			.TextArea
			.Caret
			.Position = source.TextArea.Caret.Position;

		targetScrollViewer.Offset = sourceScrollViewer.Offset;
	}

	/// <summary>
	/// Opens the lower half at the caret, the selection and the scroll position of the upper one.
	/// </summary>
	private void CopyViewToSecondaryEditor()
	{
		if (!IsSplit || SecondaryEditor is not { } secondaryEditor)
		{
			return;
		}

		CopyView(PrimaryEditor, secondaryEditor);
	}

	/// <summary>
	/// Creates the editor of a half, which takes the document and the settings of this control.
	/// </summary>
	private DocumentTextEditor CreateEditor(string name, int row)
	{
		// An editor takes no focus by default, while a focus given to it passes on to its text.
		DocumentTextEditor editor = new()
		{
			Focusable = true,
			Name = name
		};

		Grid.SetRow(editor, row);

		// Both halves show the same document the same way, as the two views of Notepad++ do.
		editor.Bind(TextEditor.DocumentProperty, this.GetObservable(DocumentProperty));

		editor.Bind(FontSizeProperty, this.GetObservable(FontSizeProperty));

		editor.Bind(TextEditor.IsReadOnlyProperty, this.GetObservable(IsReadOnlyProperty));

		editor.Bind(TextEditorBase.IsSensitiveProperty, this.GetObservable(IsSensitiveProperty));

		editor.Bind(DocumentTextEditor.ShowEndOfLineProperty, this.GetObservable(ShowEndOfLineProperty));

		editor.Bind(DocumentTextEditor.ShowSpacesProperty, this.GetObservable(ShowSpacesProperty));

		editor.Bind(DocumentTextEditor.ShowTabsProperty, this.GetObservable(ShowTabsProperty));

		editor.Bind(TextEditor.WordWrapProperty, this.GetObservable(WordWrapProperty));

		// A zoom in a half sets the size of this control, which carries it to the other half and to the owner.
		editor
			.GetObservable(FontSizeProperty)
			.Where(x => x != FontSize)
			.Subscribe(x => SetCurrentValue(FontSizeProperty, x));

		// The editor is written for the Fluent theme.
		Interaction
			.GetBehaviors(editor)
			.Add(new FluentThemeBehavior());

		// The editor marks every focus event within it handled.
		editor.AddHandler(
			GotFocusEvent,
			DocumentTextEditor_GotFocus,
			RoutingStrategies.Bubble,
			handledEventsToo: true);

		// On the tunnel a right click reaches its half before the context menu opens.
		editor.AddHandler(
			PointerPressedEvent,
			DocumentTextEditor_PointerPressed,
			RoutingStrategies.Tunnel);

		return editor;
	}
	#endregion
}
