using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using Entities.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DataOrganizer.Templates;

/// <summary>
/// Builds and caches the editor of the document of a tab: a file opened in the built-in editor or a tab of the notepad.
/// </summary>
internal sealed class DocumentTabTemplate : IDataTemplate, IViewCache
{
	#region Data
	/// <summary>
	/// Cache of <see cref="Control" />.
	/// </summary>
	private readonly Dictionary<object, Control> _cache = [];

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;
	#endregion

	#region Constructors
	public DocumentTabTemplate(IViewFactory viewFactory) => _viewFactory = viewFactory;
	#endregion

	#region Methods
	/// <inheritdoc />
	public Control? Build(object? param)
	{
		if (param is not null && _cache.TryGetValue(param, out Control? value))
		{
			return value;
		}

		if (param is FileDto file
			&& file.IsEditing
			&& CreateEditingFileControl(file, out Control? control))
		{
			_cache.Add(param, control);

			return control;
		}

		if (param is NotepadTabViewModel tab)
		{
			NotepadTabView view = _viewFactory.CreateUserControl<NotepadTabView>(tab);

			_cache.Add(param, view);

			return view;
		}

		return MissingViewPlaceholder.Create(param?.GetType().Name);
	}

	/// <inheritdoc />
	public bool Match(object? data) => data is FileDto or NotepadTabViewModel;

	/// <inheritdoc />
	public void Remove<T>(T key) where T : notnull
	{
		_cache.Remove(key, out Control? control);

		// The control holds more than its view model does, such as the highlighting of the text.
		if (control is IDisposable view)
		{
			view.Dispose();
		}

		if (control?.DataContext is not IDisposable disposable)
		{
			return;
		}

		disposable.Dispose();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a control for editing a file.
	/// </summary>
	private bool CreateEditingFileControl(FileDto file, [NotNullWhen(true)] out Control? control)
	{
		control = null;

		if (file.Kind == EntityKind.File)
		{
			EmbeddedFileEditorViewModel viewModel = _viewFactory.CreateViewModel<EmbeddedFileEditorViewModel>();

			viewModel.FileName = file.Name;

			viewModel.InitialEditorSplit = file.EditorSplit;

			viewModel.SetEditorSplitCallback = SetEditorSplit;

			Initialize(viewModel);

			control = _viewFactory.CreateUserControl<EmbeddedFileEditorView>(viewModel);

			return true;
		}
		else if (file.Kind == EntityKind.Dataset)
		{
			DatasetEditorViewModel viewModel = _viewFactory.CreateViewModel<DatasetEditorViewModel>();

			Initialize(viewModel);

			control = _viewFactory.CreateUserControl<DatasetEditorView>(viewModel);

			return true;
		}

		return false;

		void Initialize(EmbeddedEditorViewModelBase viewModel)
		{
			if (file.EncryptionStatus == Enums.Encryption.EncryptionStatus.Decrypted
				&& file.FindParent(x => x.IsPasswordKeeper()) is { } keeper)
			{
				viewModel.KeeperId = keeper.Id;
			}

			viewModel.FileId = file.Id;

			viewModel.SetEditorStateCallback = SetEditorState;

			viewModel.SetUpdatedAtCallback = SetUpdatedAt;

			viewModel.InitialEditorState = file.EditorState;

			viewModel.Initialize();
		}

		void SetEditorSplit(double? split) => file.EditorSplit = split;

		void SetEditorState(string state) => file.EditorState = state;

		void SetUpdatedAt(DateTime updatedAt) => file.UpdatedAt = updatedAt;
	}
	#endregion
}
