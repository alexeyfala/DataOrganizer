using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
using Entities.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DataOrganizer.Helpers;

internal sealed class ViewLocator : IDataTemplate, IViewCache
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
	public ViewLocator(IViewFactory viewFactory) => _viewFactory = viewFactory;
	#endregion

	#region Methods
	/// <inheritdoc />
	public Control? Build(object? param)
	{
		if (param is not null && _cache.TryGetValue(param, out Control? value))
		{
			return value;
		}

		if (param is FileModelDto file
			&& file.IsEditing
			&& CreateEditingFileControl(file, out Control? control))
		{
			_cache.Add(param, control);

			return control;
		}

		return PlugControl.Create(param?.GetType().Name);

		//return GetPlugControl(param?.GetType().Name);
	}

	/// <inheritdoc />
	public bool Match(object? data) => data is FileModelDto;

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
		FileModelDto file,
		[NotNullWhen(true)] out Control? control)
	{
		control = null;

		if (file.EntityType == EntityType.File)
		{
			EmbeddedFileEditorViewModel viewModel = _viewFactory.CreateViewModel<EmbeddedFileEditorViewModel>();

			Initialize(viewModel);

			control = _viewFactory.CreateUserControl<EmbeddedFileEditorView>(viewModel);

			return true;
		}
		else if (file.EntityType == EntityType.DataSet)
		{
			DatasetEditorViewModel viewModel = _viewFactory.CreateViewModel<DatasetEditorViewModel>();

			Initialize(viewModel);

			control = _viewFactory.CreateUserControl<DatasetEditorView>(viewModel);

			return true;
		}

		return false;

		void Initialize(EmbeddedEditorViewModelBase viewModel)
		{
			if (file.EncryptionStatus == Enums.EncryptionStatus.Decrypted
				&& file.FindParent(x => x.IsPasswordKeeper())?.SessionEncryptedDek is { } sessionEncryptedDek)
			{
				// It is important not to pass a reference to the array.
				viewModel.SessionEncryptedDek = [.. sessionEncryptedDek];
			}

			viewModel.FileId = file.Id;

			viewModel.SetPropertiesCallback = SetProperties;

			viewModel.SetUpdatedDateCallback = SetUpdatedDate;

			viewModel.InitialProperties = file.Properties;

			viewModel.Initialize();
		}

		void SetProperties(string properties) => file.Properties = properties;

		void SetUpdatedDate(DateTime updatedDate) => file.UpdatedDate = updatedDate;
	}
	#endregion
}
