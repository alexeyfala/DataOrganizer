using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit;
using CommunityToolkit.Mvvm.Input;
using System;

namespace DataOrganizer.Controls;

/// <summary>
/// <see cref="TextEditor" /> with the settings, commands and zoom common to the editors of the application.
/// </summary>
internal abstract class TextEditorBase : TextEditor
{
	#region Properties
	/// <inheritdoc />
	protected override Type StyleKeyOverride { get; } = typeof(TextEditor);
	#endregion

	#region Commands
	/// <summary>
	/// Copies the selected text.
	/// </summary>
	public RelayCommand CopyCommand { get; }

	/// <summary>
	/// Opens the search panel.
	/// </summary>
	public RelayCommand FindCommand { get; }

	/// <summary>
	/// Scrolls to the end and moves the caret to the last line.
	/// </summary>
	public RelayCommand ScrollToEndCommand { get; }

	/// <summary>
	/// Scrolls to the top and moves the caret to the first line.
	/// </summary>
	public RelayCommand ScrollToTopCommand { get; }

	/// <summary>
	/// Selects the whole text.
	/// </summary>
	public RelayCommand SelectAllCommand { get; }

	/// <summary>
	/// Changes the font size by one step in the direction of a spin.
	/// </summary>
	public RelayCommand<SpinEventArgs> SpinCommand { get; }
	#endregion

	#region Data
	/// <summary>
	/// Change of the font size per zoom step.
	/// </summary>
	private const double FontSizeStep = 0.5;

	/// <summary>
	/// The largest font size.
	/// </summary>
	private const double MaxFontSize = 64.0;

	/// <summary>
	/// The smallest font size.
	/// </summary>
	private const double MinFontSize = 6.0;
	#endregion

	#region Constructors
	protected TextEditorBase()
	{
		FontFamily = new FontFamily("Cascadia Code,Consolas,Menlo,Monospace");

		HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

		ShowLineNumbers = true;

		Options.HighlightCurrentLine = true;

		Options.EnableEmailHyperlinks = false;

		Options.AllowScrollBelowDocument = false;

		CopyCommand = new(CopySelection, CanCopySelection);

		FindCommand = new(OpenSearchPanel);

		ScrollToEndCommand = new(ScrollToLastLine);

		ScrollToTopCommand = new(ScrollToFirstLine);

		SelectAllCommand = new(SelectWholeText, CanSelectWholeText);

		SpinCommand = new(Spin);

		// On the tunnel the zoom comes before the scroll viewer inside, which would scroll the text instead.
		AddHandler(
			PointerWheelChangedEvent,
			TextEditorBase_PointerWheelChanged,
			RoutingStrategies.Tunnel);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerWheelChangedEvent" /> handler, which zooms on a notch with Ctrl.
	/// </summary>
	private void TextEditorBase_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
	{
		if (!e
			.KeyModifiers
			.HasFlag(KeyModifiers.Control))
		{
			return;
		}

		e.Handled = true;

		SpinDirection direction;

		switch (e.Delta.Y)
		{
			case > 0: // Wheel Up
				direction = SpinDirection.Increase;
				break;

			case < 0: // Wheel Down
				direction = SpinDirection.Decrease;
				break;

			default:
				return;
		}

		Zoom(direction);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="CopyCommand" />.
	/// </summary>
	private bool CanCopySelection()
	{
		return TextArea
			.Selection
			.Length > 0;
	}

	/// <summary>
	/// Validates <see cref="SelectAllCommand" />.
	/// </summary>
	private bool CanSelectWholeText() => Document is { TextLength: > 0 };

	/// <summary>
	/// Executes <see cref="ApplicationCommands.Copy" /> on the text area.
	/// </summary>
	private void CopySelection()
	{
		ApplicationCommands
			.Copy
			.Execute(null, TextArea);
	}

	/// <summary>
	/// Executes <see cref="ApplicationCommands.Find" /> on the text area.
	/// </summary>
	private void OpenSearchPanel()
	{
		ApplicationCommands
			.Find
			.Execute(null, TextArea);
	}

	/// <summary>
	/// Scrolls to the top and moves the caret to the first line.
	/// </summary>
	private void ScrollToFirstLine()
	{
		ScrollToHome();

		TextArea
			.Caret
			.Position = new(line: 0, column: 0);
	}

	/// <summary>
	/// Scrolls to the end and moves the caret to the last line.
	/// </summary>
	private void ScrollToLastLine()
	{
		ScrollToEnd();

		TextArea
			.Caret
			.Position = new(line: LineCount, column: 0);
	}

	/// <summary>
	/// Selects the whole text.
	/// </summary>
	private void SelectWholeText()
	{
		if (Document is not { } document)
		{
			return;
		}

		Select(0, document.TextLength);
	}

	/// <summary>
	/// Handles <see cref="Spinner.Spin" /> event.
	/// </summary>
	private void Spin(SpinEventArgs? e)
	{
		if (e is null)
		{
			return;
		}

		Zoom(e.Direction);
	}

	/// <summary>
	/// Changes the font size by one step, keeping it within the limits.
	/// </summary>
	private void Zoom(SpinDirection direction)
	{
		double value = direction switch
		{
			SpinDirection.Increase => FontSize + FontSizeStep,
			SpinDirection.Decrease => FontSize - FontSizeStep,
			_ => throw new NotImplementedException()
		};

		if (value < MinFontSize || value > MaxFontSize)
		{
			return;
		}

		// The current value keeps the binding that carries the size to the owner of the editor.
		SetCurrentValue(FontSizeProperty, value);
	}
	#endregion
}
