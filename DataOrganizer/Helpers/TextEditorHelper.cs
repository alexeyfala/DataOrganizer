using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using Shared.Extensions;
using System;
using System.Linq.Expressions;

namespace DataOrganizer.Helpers;

internal static class TextEditorHelper
{
	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerWheelChangedEvent" /> handler of <see cref="TextEditor" />.
	/// </summary>
	private static void Editor_PointerWheelChanged(
		PointerWheelEventArgs e,
		Func<double> currentValue,
		Expression<Func<double>> expression)
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

		IncreaseDecrease(
			direction,
			currentValue(),
			expression);
	}
	#endregion

	#region Methods
	/// <summary>
	/// Validates <see cref="Copy" />.
	/// </summary>
	public static bool CanExecuteCopy(TextArea? area)
	{
		return area?
			.Selection
			.Length > 0;
	}

	/// <summary>
	/// Validates <see cref="SelectAll" />.
	/// </summary>
	public static bool CanExecuteSelectAll(TextEditor? editor) => editor?.Text.Length > 0;

	/// <summary>
	/// Command <see cref="ApplicationCommands.Copy" />.
	/// </summary>
	public static void Copy(TextArea? area)
	{
		if (area is null)
		{
			return;
		}

		ApplicationCommands
			.Copy
			.Execute(null, area);
	}

	/// <summary>
	/// Command <see cref="ApplicationCommands.Find" />.
	/// </summary>
	public static void Find(TextArea? area)
	{
		if (area is null)
		{
			return;
		}

		ApplicationCommands
			.Find
			.Execute(null, area);
	}

	/// <summary>
	/// Scrolls the editor down.
	/// </summary>
	public static void ScrollToEnd(TextEditor? editor)
	{
		if (editor is null)
		{
			return;
		}

		editor.ScrollToEnd();

		editor
			.TextArea
			.Caret
			.Position = new(line: editor.LineCount, column: 0);
	}

	/// <summary>
	/// Scrolls the editor up.
	/// </summary>
	public static void ScrollToTop(TextEditor? editor)
	{
		if (editor is null)
		{
			return;
		}

		editor.ScrollToHome();

		editor
			.TextArea
			.Caret
			.Position = new(line: 0, column: 0);
	}

	/// <summary>
	/// Selects all text in the editor.
	/// </summary>
	public static void SelectAll(TextEditor? editor) => editor?.Select(0, editor.Text.Length);

	/// <summary>
	/// Handles <see cref="Spinner.Spin" /> event.
	/// </summary>
	public static void Spin(
		SpinEventArgs? e,
		in double currentValue,
		Expression<Func<double>> expression)
	{
		if (e is null)
		{
			return;
		}

		IncreaseDecrease(
			e.Direction,
			currentValue,
			expression);
	}

	/// <summary>
	/// Subscribes to <see cref="InputElement.PointerWheelChangedEvent" /> of <see cref="TextEditor" />.
	/// </summary>
	public static void SubscribePointerWheelChanged(
		TextEditor editor,
		Func<double> currentValue,
		Expression<Func<double>> expression)
	{
		editor.AddHandler(
			InputElement.PointerWheelChangedEvent,
			(_, e) => Editor_PointerWheelChanged(e, currentValue, expression),
			RoutingStrategies.Tunnel);
	}
	#endregion

	#region Service
	/// <summary>
	/// Increases/decreases value for <paramref name="expression"/>.
	/// </summary>
	private static void IncreaseDecrease(
		SpinDirection direction,
		in double currentValue,
		Expression<Func<double>> expression)
	{
		const double step = 0.5;

		double value = direction switch
		{
			SpinDirection.Increase => currentValue + step,
			SpinDirection.Decrease => currentValue - step,
			_ => throw new NotImplementedException()
		};

		if (value < 6.0 || value > 64.0)
		{
			return;
		}

		expression.SetValue(value);
	}
	#endregion
}
