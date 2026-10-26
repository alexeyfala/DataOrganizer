using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using Hierarchy.Dto;
using System;
using System.Collections.Generic;

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

	/// <inheritdoc cref="IEmbeddedEditorFactory" />
	private readonly IEmbeddedEditorFactory _editorFactory;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;
	#endregion

	#region Constructors
	public DocumentTabTemplate(IEmbeddedEditorFactory editorFactory, IViewFactory viewFactory)
	{
		_editorFactory = editorFactory;

		_viewFactory = viewFactory;
	}
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
			&& _editorFactory.Create(file) is { } viewModel)
		{
			Control control = CreateEditorView(viewModel);

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
	public void Remove(object key)
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
	/// Creates the view of the editor of a file.
	/// </summary>
	private Control CreateEditorView(EmbeddedEditorViewModelBase viewModel)
	{
		return viewModel switch
		{
			DatasetEditorViewModel => _viewFactory.CreateUserControl<DatasetEditorView>(viewModel),
			EmbeddedFileEditorViewModel => _viewFactory.CreateUserControl<EmbeddedFileEditorView>(viewModel),
			_ => MissingViewPlaceholder.Create(viewModel.GetType().Name)
		};
	}
	#endregion
}
