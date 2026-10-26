using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Messages.Editor;
using Hierarchy.Dto;
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
	private void ShowInEditor(Guid id) => _messenger.Send(new ShowInEditorMessage(id));
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

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;
	#endregion

	#region Constructors
	protected FileListViewModelBase(
		IContentCopier contentCopier,
		IMessenger messenger,
		ITaskExceptionHandler exceptionHandler)
	{
		_contentCopier = contentCopier;

		_exceptionHandler = exceptionHandler;

		_messenger = messenger;
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
