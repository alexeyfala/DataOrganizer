using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <c>ConsoleWindow</c>.
/// </summary>
public sealed partial class ConsoleViewModel : ObservableDisposableBase
{
	#region Properties
	/// <inheritdoc cref="FileProperties.FontSize" />
	[ObservableProperty]
	public partial double FontSize { get; set; } = 14.0;

	/// <summary>
	/// Indicates that recording should be paused.
	/// </summary>
	[ObservableProperty]
	public partial bool IsPaused { get; set; }

	/// <summary>
	/// <c>True</c> when settings are saved.
	/// </summary>
	public bool IsSaved { get; set; }

	/// <inheritdoc cref="FileProperties.IsWordWrap" />
	[ObservableProperty]
	public partial bool IsWordWrap { get; set; }

	/// <summary>
	/// A reference to a method for writing a line of text.
	/// </summary>
	public Action<string> WriteCallback => Write;
	#endregion

	#region Commands
	/// <inheritdoc cref="TextEditorHelper.Copy" />
	public RelayCommand<TextArea> CopyCommand { get; } = new(TextEditorHelper.Copy, TextEditorHelper.CanCopy);

	/// <inheritdoc cref="TextEditorHelper.Find" />
	public RelayCommand<TextArea> FindCommand { get; } = new(TextEditorHelper.Find);

	/// <inheritdoc cref="TextEditorHelper.ScrollToEnd" />
	public RelayCommand<TextEditor> ScrollToEndCommand { get; } = new(TextEditorHelper.ScrollToEnd);

	/// <inheritdoc cref="TextEditorHelper.ScrollToTop" />
	public RelayCommand<TextEditor> ScrollToTopCommand { get; } = new(TextEditorHelper.ScrollToTop);

	/// <inheritdoc cref="TextEditorHelper.SelectAll" />
	public RelayCommand<TextEditor> SelectAllCommand { get; } = new(TextEditorHelper.SelectAll, TextEditorHelper.CanSelectAll);

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

	/// <inheritdoc cref="IProcessUtils.OpenAppDirectory()" />
	[RelayCommand]
	private void OpenAppDirectory() => _processUtils.OpenAppDirectory();

	/// <summary>
	/// Opens the application database directory.
	/// </summary>
	[RelayCommand]
	private void OpenDatabaseDirectory() => _processUtils.OpenDirectory(_appEnvironment.DatabaseDirectoryPath);
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

	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="Lock" />
	private readonly Lock _mutex = new();

	/// <inheritdoc cref="IProcessUtils" />
	private readonly IProcessUtils _processUtils;

	/// <summary>
	/// Record buffer.
	/// </summary>
	private readonly List<string> _recordsBuffer = [];

	/// <summary>
	/// Reference to <see cref="TextEditor" />.
	/// </summary>
	private TextEditor? _editor;
	#endregion

	#region Constructors
	public ConsoleViewModel(
		IAppEnvironment appEnvironment,
		IDispatcherAccessor dispatcher,
		IProcessUtils processUtils)
	{
		_appEnvironment = appEnvironment;

		_dispatcher = dispatcher;

		_processUtils = processUtils;

		SpinCommand = new(e => TextEditorHelper.Spin(e, FontSize, () => FontSize));
	}
	#endregion

	#region Helpers
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
