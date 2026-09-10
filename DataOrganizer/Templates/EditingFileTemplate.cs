using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
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
/// Builds and caches the editor control for a file opened in the built-in editor.
/// </summary>
internal sealed class EditingFileTemplate : IDataTemplate, IViewCache
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
	public EditingFileTemplate(IViewFactory viewFactory) => _viewFactory = viewFactory;
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

		return MissingViewPlaceholder.Create(param?.GetType().Name);
	}

	/// <inheritdoc />
	public bool Match(object? data) => data is FileDto;

	/// <inheritdoc />
	public void Remove<T>(T key) where T : notnull
	{
		_cache.Remove(key, out Control? control);

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
	private bool CreateEditingFileControl(
		FileDto file,
		[NotNullWhen(true)] out Control? control)
	{
		control = null;

		if (file.Kind == EntityKind.File)
		{
			EmbeddedFileEditorViewModel viewModel = _viewFactory.CreateViewModel<EmbeddedFileEditorViewModel>();

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

		void SetEditorState(string state) => file.EditorState = state;

		void SetUpdatedAt(DateTime updatedAt) => file.UpdatedAt = updatedAt;
	}
	#endregion
}
