using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Runtime;
using Diagnostics.Interfaces;
using FileSystemOperations.Interfaces;
using System;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Threading.Enums;
using Threading.Interfaces;

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
	public ILogText Document { get; }

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
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Clears the log.
	/// </summary>
	[RelayCommand]
	private void Clear() => Document.Clear();

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

		ReadFromLog();
	}
	#endregion

	#region Data
	/// <summary>
	/// The largest number of lines the log keeps.
	/// </summary>
	private const int MaxLineCount = 999_999;

	/// <inheritdoc cref="IAppEnvironment" />
	private readonly IAppEnvironment _appEnvironment;

	/// <inheritdoc cref="IConsoleLog" />
	private readonly IConsoleLog _consoleLog;

	/// <inheritdoc cref="IDirectoryAccessor" />
	private readonly IDirectoryAccessor _directoryAccessor;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;
	#endregion

	#region Constructors
	public ConsoleViewModel(
		IAppEnvironment appEnvironment,
		IConsoleLog consoleLog,
		IDirectoryAccessor directoryAccessor,
		IDispatcherAccessor dispatcher,
		ILogText logText)
	{
		_appEnvironment = appEnvironment;

		_consoleLog = consoleLog;

		_directoryAccessor = directoryAccessor;

		_dispatcher = dispatcher;

		Document = logText;

		// The records written before the console existed come with the first read. The pause is checked on the UI
		// thread, which owns the document and sets the pause, so a record posted just before a pause waits with the
		// ones written after it.
		_consoleLog
			.Written
			.StartWith(Unit.Default)
			.Subscribe(_ => _dispatcher.Post(ReadFromLog, UiPriority.Background))
			.DisposeWith(_disposables);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Moves the records of the console log into <see cref="Document" /> unless recording is paused.
	/// </summary>
	private void ReadFromLog()
	{
		if (IsPaused)
		{
			return;
		}

		foreach (string record in _consoleLog.TakeAll())
		{
			Document.Append(record, MaxLineCount);
		}
	}
	#endregion
}
