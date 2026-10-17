using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Views;
using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

public abstract partial class FileListViewModelBase : ObservableDisposableBase
{
	#region Properties
	/// <summary>
	/// Objects to highlight where the view shows them.
	/// </summary>
	public IObservable<ExplorerItemDtoBase> HighlightSignal => _highlightSignal;
	#endregion

	#region Auto-Generated Commands
	/// <inheritdoc cref="IContentCopier.CopyAsync" />
	[RelayCommand(CanExecute = nameof(CanCopyContent))]
	private void CopyContent(FileDto? file)
	{
		if (file is null)
		{
			return;
		}

		SelectFile(file);

		_exceptionHandler.Watch(CopyContentAsync(file));
	}

	/// <summary>
	/// Displays object in "Editor".
	/// </summary>
	[RelayCommand]
	private void ShowInEditor(Guid id) => _mainWindowLauncher.ShowInEditor(id);
	#endregion

	#region Data
	/// <inheritdoc cref="IContentCopier" />
	private readonly IContentCopier _contentCopier;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <summary>
	/// Objects to highlight where the view shows them.
	/// </summary>
	private readonly Subject<ExplorerItemDtoBase> _highlightSignal = new();

	/// <inheritdoc cref="IMainWindowLauncher" />
	private readonly IMainWindowLauncher _mainWindowLauncher;
	#endregion

	#region Constructors
	protected FileListViewModelBase(
		IContentCopier contentCopier,
		IMainWindowLauncher mainWindowLauncher,
		ITaskExceptionHandler exceptionHandler)
	{
		_contentCopier = contentCopier;

		_exceptionHandler = exceptionHandler;

		_mainWindowLauncher = mainWindowLauncher;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Selects a file of the list.
	/// </summary>
	protected abstract void SelectFile(FileDto file);
	#endregion

	#region Helpers
	/// <summary>
	/// Validates <see cref="CopyContentCommand" />.
	/// </summary>
	private static bool CanCopyContent(FileDto? file) => file?.IsOpened() != true;

	/// <summary>
	/// Copies the contents of a file of the list and highlights it; a shown copy history moves it at its next showing.
	/// </summary>
	private async Task CopyContentAsync(FileDto file)
	{
		if (!await _contentCopier
			.CopyAsync(file, showAtOnce: false)
			.ConfigureAwait(true))
		{
			return;
		}

		_highlightSignal.OnNext(file);
	}
	#endregion
}
