using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.DTO;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Windows;
using Serilog.Events;
using Shared.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="ConsoleWindow" />.
/// </summary>
public sealed partial class ConsoleViewModel : ObservableDisposable
{
	#region Properties
	/// <summary>
	/// Returns <c>True</c> if settings are saved.
	/// </summary>
	public bool IsSaved { get; set; }

	/// <summary>
	/// A reference to a method for writing a line of text.
	/// </summary>
	public Action<string> WriteCallback => Write;
	#endregion

	#region Auto-Generated Properties
	/// <inheritdoc cref="FileProperties.FontSize" />
	[ObservableProperty]
	private double _fontSize = 14.0;

	/// <summary>
	/// Indicates that recording should be paused.
	/// </summary>
	[ObservableProperty]
	private bool _isPaused;

	/// <inheritdoc cref="FileProperties.IsWordWrap" />
	[ObservableProperty]
	private bool _isWordWrap;
	#endregion

	#region Commands
	/// <inheritdoc cref="TextEditorHelper.Copy" />
	public RelayCommand<TextArea> CopyCommand { get; } = new(TextEditorHelper.Copy, TextEditorHelper.CanExecuteCopy);

	/// <inheritdoc cref="TextEditorHelper.Find" />
	public RelayCommand<TextArea> FindCommand { get; } = new(TextEditorHelper.Find);

	/// <inheritdoc cref="TextEditorHelper.ScrollToEnd" />
	public RelayCommand<TextEditor> ScrollToEndCommand { get; } = new(TextEditorHelper.ScrollToEnd);

	/// <inheritdoc cref="TextEditorHelper.ScrollToTop" />
	public RelayCommand<TextEditor> ScrollToTopCommand { get; } = new(TextEditorHelper.ScrollToTop);

	/// <inheritdoc cref="TextEditorHelper.SelectAll" />
	public RelayCommand<TextEditor> SelectAllCommand { get; } = new(TextEditorHelper.SelectAll, TextEditorHelper.CanExecuteSelectAll);

	/// <inheritdoc cref="TextEditorHelper.Spin" />
	public RelayCommand<SpinEventArgs> SpinCommand { get; }
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Clears <see cref="TextEditor" />.
	/// </summary>
	[RelayCommand]
	private static void Clear(TextEditor? editor) => editor?.Clear();

	/// <summary>
	/// Handles the <see cref="Control.Loaded" /> event of <see cref="TextEditor" />.
	/// </summary>
	[RelayCommand]
	private void EditorLoaded(TextEditor? editor)
	{
		if (editor is null)
		{
			return;
		}

		_editor = editor;

		TextEditorHelper.SubscribePointerWheelChanged(
			editor,
			() => FontSize,
			() => FontSize);

		ApplyEditorSettings(editor);

		ReadFromBuffer();
	}

	/// <summary>
	/// Opens the application data directory.
	/// </summary>
	[RelayCommand]
	private void OpenAppDataDirectory() => _processUtils?.OpenDirectory(AppUtils.AppDataDirectoryPath);

	/// <inheritdoc cref="IProcessUtils.OpenAppDirectory()" />
	[RelayCommand]
	private void OpenAppDirectory() => _processUtils?.OpenAppDirectory();
	#endregion

	#region Partial
	/// <summary>
	/// Called when <see cref="IsPaused" /> changes.
	/// </summary>
	partial void OnIsPausedChanged(bool value)
	{
		if (value)
		{
			return;
		}

		ReadFromBuffer();
	}
	#endregion

	#region Data
	/// <summary>
	/// First line position.
	/// </summary>
	private static readonly TextViewPosition _firstLinePosition = new();

	/// <inheritdoc cref="IDispatcher" />
	private readonly IDispatcher _dispatcher;

	/// <inheritdoc cref="Lock" />
	private readonly Lock _mutex = new();

	/// <summary>
	/// Record buffer.
	/// </summary>
	private readonly List<string> _recordsBuffer = [];

	/// <summary>
	/// Reference to <see cref="TextEditor" />.
	/// </summary>
	private TextEditor? _editor;

	/// <inheritdoc cref="IProcessUtils" />
	private IProcessUtils? _processUtils;
	#endregion

	#region Constructors
	public ConsoleViewModel(IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;

		SpinCommand = new(e => TextEditorHelper.Spin(e, FontSize, () => FontSize));
	}
	#endregion

	#region Methods
	/// <summary>
	/// Performs dependency injection <see cref="IProcessUtils" />.
	/// </summary>
	public void InjectReference(IProcessUtils target) => _processUtils = target;
	#endregion

	#region Service
	/// <summary>
	/// Applies settings to <see cref="TextEditor" />.
	/// </summary>
	private static void ApplyEditorSettings(TextEditor editor)
	{
		editor
			.TextArea
			.TextView
			.Margin = new Thickness(6.0, 0.0);

		editor
			.TextArea
			.TextView
			.LineTransformers
			.Add(new WordOccurrenceTransformer(
				LogEventLevel.Debug.ToShort(),
				LogEventLevel.Debug.ToBrush()));

		editor
			.TextArea
			.TextView
			.LineTransformers
			.Add(new WordOccurrenceTransformer(
				LogEventLevel.Information.ToShort(),
				LogEventLevel.Information.ToBrush()));

		editor
			.TextArea
			.TextView
			.LineTransformers
			.Add(new WordOccurrenceTransformer(
				LogEventLevel.Warning.ToShort(),
				LogEventLevel.Warning.ToBrush()));

		editor
			.TextArea
			.TextView
			.LineTransformers
			.Add(new WordOccurrenceTransformer(
				LogEventLevel.Error.ToShort(),
				LogEventLevel.Error.ToBrush()));

		editor
			.Options
			.HighlightCurrentLine = true;

		editor
			.Options
			.EnableEmailHyperlinks = false;

		editor
			.Options
			.AllowScrollBelowDocument = false;
	}

	/// <summary>
	/// Removes leading lines.
	/// </summary>
	private static void RemoveStartLines(TextEditor editor, int maxLines)
	{
		if (editor.LineCount <= maxLines)
		{
			return;
		}

		bool isReadOnly = editor.IsReadOnly;

		try
		{
			editor.IsReadOnly = false;

			while (editor.LineCount > maxLines)
			{
				editor
					.TextArea
					.Caret
					.Position = _firstLinePosition;

				AvaloniaEditCommands
					.DeleteLine
					.Execute(null, editor.TextArea);
			}
		}
		finally
		{
			editor.IsReadOnly = isReadOnly;
		}
	}

	/// <summary>
	/// Reads records from the buffer if it is not empty.
	/// </summary>
	private void ReadFromBuffer()
	{
		lock (_mutex)
		{
			if (_editor is null || _recordsBuffer.Count == 0)
			{
				return;
			}

			_recordsBuffer.ForEach(_editor.AppendText);

			_recordsBuffer.Clear();
		}
	}

	/// <summary>
	/// Writes a line of text.
	/// </summary>
	private void Write(string value)
	{
		lock (_mutex)
		{
			if (_editor is null || IsPaused)
			{
				_recordsBuffer.Add(value);

				return;
			}

			_dispatcher.Post(() =>
			{
				ReadFromBuffer();

				_editor.AppendText(value);

				RemoveStartLines(_editor, 999_999);

				_editor.ScrollToEnd();
			}, DispatcherPriority.Background);
		}
	}
	#endregion
}
