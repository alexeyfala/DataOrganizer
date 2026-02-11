using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using DataOrganizer.Extensions;
using System;
using System.Linq.Expressions;

namespace DataOrganizer.Helpers;

internal static class TextEditorHelper
{
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
		Expression<Func<double>> property)
	{
		if (e is null)
		{
			return;
		}

		e.Direction.IncreaseDecrease(currentValue, property);
	}
	#endregion
}
