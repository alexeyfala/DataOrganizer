using Avalonia.Threading;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Storage;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DataOrganizer.ViewModels.Windows;

/// <summary>
/// View model for <c>ConsoleWindow</c>.
/// </summary>
public sealed partial class ConsoleViewModel : ObservableDisposableBase
{
	#region Properties
	/// <summary>
	/// The log records shown in the console.
	/// </summary>
	public TextDocument Document { get; } = new()
	{
		// An undo history of the log would keep every record and every removed line in memory.
		UndoStack =
		{
			SizeLimit = 0
		}
	};

	/// <inheritdoc cref="FileEditorState.FontSize" />
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

	/// <inheritdoc cref="FileEditorState.WordWrap" />
	[ObservableProperty]
	public partial bool WordWrap { get; set; }

	/// <summary>
	/// A reference to a method for writing a line of text.
	/// </summary>
	public Action<string> WriteCallback => Write;
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Clears the log.
	/// </summary>
	[RelayCommand]
	private void Clear() => Document.Remove(0, Document.TextLength);

	/// <summary>
	/// Opens the application data directory.
	/// </summary>
	[RelayCommand]
	private void OpenAppDataDirectory() => _directoryAccessor.OpenDirectory(_appEnvironment.AppDataDirectoryPath);

	/// <inheritdoc cref="IDirectoryAccessor.OpenAppDirectory" />
	[RelayCommand]
	private void OpenAppDirectory() => _directoryAccessor.OpenAppDirectory();
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
	/// The largest number of lines the log keeps.
	/// </summary>
	private const int MaxLineCount = 999_999;

	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IDirectoryAccessor" />
	private readonly IDirectoryAccessor _directoryAccessor;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="Lock" />
	private readonly Lock _mutex = new();

	/// <summary>
	/// Log records waiting to be written into <see cref="Document" />.
	/// </summary>
	private readonly List<string> _recordsBuffer = [];
	#endregion

	#region Constructors
	public ConsoleViewModel(
		IAppEnvironment appEnvironment,
		IDirectoryAccessor directoryAccessor,
		IDispatcherAccessor dispatcher)
	{
		_appEnvironment = appEnvironment;

		_directoryAccessor = directoryAccessor;

		_dispatcher = dispatcher;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Removes leading lines of <paramref name="document" />, so that at most <paramref name="maxLines" /> remain.
	/// </summary>
	internal static void RemoveStartLines(TextDocument document, int maxLines)
	{
		if (document.LineCount <= maxLines)
		{
			return;
		}

		DocumentLine firstKeptLine = document.GetLineByNumber(document.LineCount - maxLines + 1);

		document.Remove(0, firstKeptLine.Offset);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Appends a record to the end of the log.
	/// </summary>
	private void Append(string value)
	{
		Document.Insert(Document.TextLength, value);

		RemoveStartLines(Document, MaxLineCount);
	}

	/// <summary>
	/// Moves the records from the buffer into the log unless recording is paused.
	/// </summary>
	private void ReadFromBuffer()
	{
		if (IsPaused)
		{
			return;
		}

		lock (_mutex)
		{
			_recordsBuffer.ForEach(Append);

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
			_recordsBuffer.Add(value);
		}

		// The pause is checked on the UI thread, which owns the document and sets the pause,
		// so a record posted just before a pause waits with the ones written after it.
		_dispatcher.Post(ReadFromBuffer, DispatcherPriority.Background);
	}
	#endregion
}
