using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AvaloniaEdit.Rendering;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Messages.Documents;
using DataOrganizer.Services.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Controls.Text;

/// <summary>
/// Folding of the blocks of the text of a <see cref="DocumentTextEditor" /> by the rules of its language, with the chords
/// of Visual Studio that fold them and the tip that shows the text a folded block hides.
/// </summary>
internal sealed class DocumentFoldingController
{
	#region Properties
	/// <summary>
	/// <c>True</c> while the text folds by the rules of its language.
	/// </summary>
	public bool CanFold => _folding is not null;

	/// <summary>
	/// <c>True</c> while a block of the text is unfolded.
	/// </summary>
	public bool CanFoldAll => _folding is { HasUnfoldedBlocks: true };

	/// <summary>
	/// <c>True</c> while a block of the text starts or goes on in the caret line.
	/// </summary>
	public bool CanToggleFolding => _folding?.FindBlock(_editor.TextArea.Caret.Line) is not null;

	/// <summary>
	/// <c>True</c> while a block of the text is folded.
	/// </summary>
	public bool CanUnfoldAll => _folding is { HasFoldedBlocks: true };
	#endregion

	#region Data
	/// <summary>
	/// Modifier of the keys of the folding chords, the same on every system, as macOS keeps ⌘M for minimizing a window.
	/// </summary>
	private const KeyModifiers ChordModifier = KeyModifiers.Control;

	/// <summary>
	/// The editor whose text folds.
	/// </summary>
	private readonly DocumentTextEditor _editor;

	/// <summary>
	/// Folding of the blocks of the text; <c>null</c> while the text is plain.
	/// </summary>
	private SyntaxFolding? _folding;

	/// <summary>
	/// <c>True</c> after the first key of a chord, while its second key is awaited.
	/// </summary>
	private bool _isChordStarted;
	#endregion

	#region Constructors
	public DocumentFoldingController(DocumentTextEditor editor)
	{
		_editor = editor;

		// The chords of the folding, which key bindings cannot express, reach the editor on the tunnel before its text,
		// and so do the keys that key bindings have taken, as those end a chord too.
		editor.AddHandler(
			InputElement.KeyDownEvent,
			Editor_KeyDown,
			RoutingStrategies.Tunnel,
			handledEventsToo: true);

		// A chord ends with the focus, so that a key typed on the return is not taken for its second key.
		editor
			.GetObservable(InputElement.IsKeyboardFocusWithinProperty)
			.Subscribe(IsKeyboardFocusWithinProperty_Changed);

		TextView textView = editor.TextArea.TextView;

		// The tip of a folded block takes the look of the application rather than the Fluent one the editor brings for itself.
		if (Application
			.Current?
			.TryFindResource(typeof(ToolTip), out object? toolTipTheme) == true)
		{
			textView.Resources[typeof(ToolTip)] = toolTipTheme;
		}

		// The tip opens and closes with the hover over a box of the text, not with the pointer over the whole view.
		ToolTip.SetServiceEnabled(textView, false);

		textView.PointerHover += TextView_PointerHover;

		textView.PointerHoverStopped += TextView_PointerHoverStopped;

		// A double click on a box unfolds its block, which the tip would still cover, and the presses on a box are handled.
		textView.AddHandler(
			InputElement.PointerPressedEvent,
			TextView_PointerPressed,
			RoutingStrategies.Tunnel,
			handledEventsToo: true);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.KeyDownEvent" /> handler of the editor, which folds on the chords of Visual Studio:
	/// Ctrl+M, Ctrl+M for the block of the caret line and Ctrl+M, Ctrl+L for every block.
	/// </summary>
	private void Editor_KeyDown(object? sender, KeyEventArgs e)
	{
		// A modifier held through a chord repeats its key, and a modifier pressed anew comes before the second key.
		if (IsModifierKey(e.Key))
		{
			return;
		}

		// Key bindings take their keys before the event is raised, and such a key ends a chord without acting in it.
		if (e.Handled)
		{
			_isChordStarted = false;

			return;
		}

		if (!_isChordStarted)
		{
			if (e.Key != Key.M || e.KeyModifiers != ChordModifier)
			{
				return;
			}

			_isChordStarted = true;

			e.Handled = true;

			return;
		}

		_isChordStarted = false;

		// The second key works with the modifier of the first one or without it, and any other key acts as usual.
		if (e.KeyModifiers != KeyModifiers.None && e.KeyModifiers != ChordModifier)
		{
			return;
		}

		switch (e.Key)
		{
			case Key.L:
				ToggleAllFoldings();
				break;

			case Key.M:
				ToggleFolding();
				break;

			default:
				return;
		}

		// A chord with nothing to fold does nothing, rather than type its second key into the text.
		e.Handled = true;
	}

	/// <summary>
	/// <see cref="InputElement.PointerExited" /> event handler of the host of a tip that took the pointer.
	/// </summary>
	private void FoldingTipHost_PointerExited(object? sender, PointerEventArgs e) => CloseFoldingTip();

	/// <summary>
	/// <see cref="InputElement.PointerPressed" /> event handler of the host of a tip that took the pointer.
	/// </summary>
	private void FoldingTipHost_PointerPressed(object? sender, PointerPressedEventArgs e) => CloseFoldingTip();

	/// <summary>
	/// <see cref="InputElement.IsKeyboardFocusWithinProperty" /> changed handler of the editor.
	/// </summary>
	private void IsKeyboardFocusWithinProperty_Changed(bool value)
	{
		if (value)
		{
			return;
		}

		_isChordStarted = false;
	}

	/// <summary>
	/// <see cref="TextView.PointerHover" /> event handler, which shows the text that the box of a folded block under the
	/// pointer hides.
	/// </summary>
	private void TextView_PointerHover(object? sender, PointerEventArgs e)
	{
		TextView textView = _editor.TextArea.TextView;

		// The text of an encrypted file stays in the text area.
		if (_editor.IsSensitive || _folding?.FindHiddenText(e.GetPosition(textView)) is not { } text)
		{
			return;
		}

		ToolTip.SetTip(textView, new TextBlock
		{
			FontFamily = _editor.TextArea.FontFamily,
			Text = text
		});

		ToolTip.SetIsOpen(textView, true);
	}

	/// <summary>
	/// <see cref="TextView.PointerHoverStopped" /> event handler, which closes the tip, unless the tip opened under the
	/// pointer and so took the pointer from the view.
	/// </summary>
	private void TextView_PointerHoverStopped(object? sender, PointerEventArgs e)
	{
		TextView textView = _editor.TextArea.TextView;

		// Closed, such a tip would leave the pointer to the view, whose hover would open it again, so it stays until the
		// pointer leaves it or presses it, as in Visual Studio Code.
		if (!textView.IsPointerOver && FindFoldingTipHost(e.GetPosition(textView)) is { } host)
		{
			host.PointerExited += FoldingTipHost_PointerExited;

			host.PointerPressed += FoldingTipHost_PointerPressed;

			return;
		}

		CloseFoldingTip();
	}

	/// <summary>
	/// <see cref="InputElement.PointerPressedEvent" /> handler of the text view.
	/// </summary>
	private void TextView_PointerPressed(object? sender, PointerPressedEventArgs e) => CloseFoldingTip();
	#endregion

	#region Methods
	/// <summary>
	/// Folds every block and moves the caret out of the folded text.
	/// </summary>
	public void FoldAll()
	{
		if (_folding is not { } folding)
		{
			return;
		}

		folding.FoldAll();

		MoveCaretOutOfFolding();

		ReportFolding();
	}

	/// <summary>
	/// Returns the offsets where the folded blocks start, or the unfolded ones; none while the text does not fold.
	/// </summary>
	public int[] GetBlockStarts(bool isFolded) => _folding?.GetBlockStarts(isFolded) ?? [];

	/// <summary>
	/// Removes the folding, and the folded text comes back into view.
	/// </summary>
	public void Remove()
	{
		if (_folding is not { } folding)
		{
			return;
		}

		_folding = null;

		// The tip shows the text of a block that goes away with the folding.
		CloseFoldingTip();

		folding.Dispose();
	}

	/// <summary>
	/// Folds or unfolds the blocks that start at the offsets and turns the other blocks the other way; an offset where no
	/// block starts is skipped.
	/// </summary>
	public void SetBlocksFolded(IEnumerable<int> starts, bool isFolded) => _folding?.SetBlocksFolded(starts, isFolded);

	/// <summary>
	/// Folds or unfolds the innermost block of the caret line, and moves the caret out of it when it folds.
	/// </summary>
	public void ToggleFolding()
	{
		if (_folding?.FindBlock(_editor.TextArea.Caret.Line) is not { } block)
		{
			return;
		}

		block.IsFolded = !block.IsFolded;

		MoveCaretOutOfFolding();

		ReportFolding();
	}

	/// <summary>
	/// Unfolds every block.
	/// </summary>
	public void UnfoldAll()
	{
		if (_folding is not { } folding)
		{
			return;
		}

		folding.UnfoldAll();

		ReportFolding();
	}

	/// <summary>
	/// Brings the folding in line with a language and the document of the editor; <c>null</c> leaves the text plain.
	/// </summary>
	public void Update(string? language)
	{
		if (language == _folding?.Language && _editor.Document == _folding?.Document)
		{
			return;
		}

		// The folded text comes back into view with the folding, while neither the caret nor the view moves.
		bool hadFoldedBlocks = _folding is { HasFoldedBlocks: true };

		Remove();

		if (hadFoldedBlocks)
		{
			ReportFolding();
		}

		if (language is null || SyntaxRegistry
			.Instance
			.FindFoldingRules(language) is not { } rules)
		{
			return;
		}

		_folding = new SyntaxFolding(_editor.TextArea, language, rules);
	}

	/// <summary>
	/// Finds the blocks that fold with a pass over all lines.
	/// </summary>
	public void UpdateBlocks() => _folding?.Update();
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> for the key of a modifier.
	/// </summary>
	private static bool IsModifierKey(Key key)
	{
		return key is Key.LeftCtrl
			or Key.RightCtrl
			or Key.LeftShift
			or Key.RightShift
			or Key.LeftAlt
			or Key.RightAlt
			or Key.LWin
			or Key.RWin;
	}

	/// <summary>
	/// Takes away the tip of a folded block with its text, which closes the tip.
	/// </summary>
	private void CloseFoldingTip() => ToolTip.SetTip(_editor.TextArea.TextView, null);

	/// <summary>
	/// Returns the host of the open tip, the window or the element that shows it, when it covers a point of the view;
	/// <c>null</c> when no open tip covers the point.
	/// </summary>
	private InputElement? FindFoldingTipHost(Point point)
	{
		TextView textView = _editor.TextArea.TextView;

		// A popup shows in a window of its own, or in the overlay of the window where windows of their own are not at hand.
		if ((ToolTip.GetTip(textView) as Visual)?
			.GetVisualAncestors()
			.FirstOrDefault(static x => x is PopupRoot or OverlayPopupHost) is not InputElement host)
		{
			return null;
		}

		// A tip at the pointer starts right where the pointer stands, which rounding to pixels may move by one pixel.
		PixelVector rounding = new(1, 1);

		// The view and the tip may stand in windows of their own, so they meet on the screen.
		PixelRect bounds = new(
			host.PointToScreen(default) - rounding,
			host.PointToScreen(new Point(host.Bounds.Width, host.Bounds.Height)) + rounding);

		return bounds.Contains(textView.PointToScreen(point)) ? host : null;
	}

	/// <summary>
	/// Moves the caret out of the folded block that hides it to the start of the block, which stays in view.
	/// </summary>
	private void MoveCaretOutOfFolding()
	{
		if (_folding?.FindFoldedBlock(_editor.TextArea.Caret.Offset) is not { } block)
		{
			return;
		}

		// A selection left behind would reach into the hidden text.
		_editor.Select(block.StartOffset, 0);

		_editor
			.TextArea
			.Caret
			.BringCaretToView();
	}

	/// <summary>
	/// Tells that blocks of the editor folded or unfolded.
	/// </summary>
	private void ReportFolding()
	{
		WeakReferenceMessenger
			.Default
			.Send(new FoldingChangedMessage(_editor));
	}

	/// <summary>
	/// Unfolds every block when a block is folded, and folds every block otherwise, as Visual Studio does.
	/// </summary>
	private void ToggleAllFoldings()
	{
		if (_folding is { HasFoldedBlocks: true })
		{
			UnfoldAll();

			return;
		}

		FoldAll();
	}
	#endregion
}
