using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO;
using System;

namespace DataOrganizer.Abstract;

public abstract partial class TextEditorViewModelBase : EditorViewModelBase
{
	#region Auto-Generated Properties
	/// <inheritdoc cref="FileProperties.FontSize" />
	[ObservableProperty]
	private double _fontSize = 14.0;

	/// <inheritdoc cref="FileProperties.IsWordWrap" />
	[ObservableProperty]
	private bool _isWordWrap;
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Command <see cref="ApplicationCommands.Copy" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanExecuteCopy))]
	private static void Copy(TextArea? area)
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
	[RelayCommand]
	private static void Find(TextArea? area)
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
	[RelayCommand]
	private static void ScrollToEnd(TextEditor? editor)
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
	[RelayCommand]
	private static void ScrollToTop(TextEditor? editor)
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
	[RelayCommand(CanExecute = nameof(CanExecuteSelectAll))]
	private static void SelectAll(TextEditor? editor) => editor?.Select(0, editor.Text.Length);

	/// <summary>
	/// Handles <see cref="Spinner.Spin" /> event.
	/// </summary>
	[RelayCommand]
	private void Spin(SpinEventArgs? e)
	{
		if (e is null)
		{
			return;
		}

		ChangeFontSize(e.Direction);
	}
	#endregion

	#region Constructors
	protected TextEditorViewModelBase(App app) : base(app)
	{
	}
	#endregion

	#region Methods
	/// <summary>
	/// Subscribes to <see cref="InputElement.PointerWheelChangedEvent" /> of <see cref="TextEditor" />.
	/// </summary>
	protected void SubscribePointerWheelChanged(TextEditor editor)
	{
		editor.AddHandler(
			InputElement.PointerWheelChangedEvent,
			Editor_PointerWheelChanged,
			RoutingStrategies.Tunnel);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerWheelChangedEvent" /> handler of <see cref="TextEditor" />.
	/// </summary>
	private void Editor_PointerWheelChanged(object? _, PointerWheelEventArgs e)
	{
		if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
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

		ChangeFontSize(direction);
	}
	#endregion

	#region Service
	/// <summary>
	/// Validates <see cref="CopyCommand" />.
	/// </summary>
	private static bool CanExecuteCopy(TextArea? area) => area?.Selection.Length > 0;

	/// <summary>
	/// Validates <see cref="SelectAllCommand" />.
	/// </summary>
	private static bool CanExecuteSelectAll(TextEditor? editor) => editor?.Text.Length > 0;

	/// <summary>
	/// Changes <see cref="FontSize" />.
	/// </summary>
	private void ChangeFontSize(in SpinDirection direction)
	{
		const double step = 0.5;

		double value = direction switch
		{
			SpinDirection.Increase => FontSize + step,
			SpinDirection.Decrease => FontSize - step,
			_ => throw new NotImplementedException()
		};

		if (value < 6.0 || value > 64.0)
		{
			return;
		}

		FontSize = value;
	}
	#endregion
}
