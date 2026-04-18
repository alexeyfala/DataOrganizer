using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.DependencyInjection;
using DataOrganizer.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using Entities.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DataOrganizer.Helpers;

internal sealed class ViewLocator : IDataTemplate
{
	#region Data
	/// <summary>
	/// Cache of <see cref="Control" />.
	/// </summary>
	private static readonly Dictionary<object, Control> _cache = [];

	/// <inheritdoc cref="IViewFactory" />
	private static readonly IViewFactory _viewFactory = Ioc
		.Default
		.GetRequiredService<IViewFactory>();
	#endregion

	#region Methods
	/// <summary>
	/// Removes control from cache.
	/// </summary>
	public static void RemoveFromCache<T>(T key) where T : notnull
	{
		_cache.Remove(key, out Control? control);

		if (control?.DataContext is not IDisposable disposable)
		{
			return;
		}

		disposable.Dispose();
	}

	/// <inheritdoc />
	public Control? Build(object? param)
	{
		if (param is not null && _cache.TryGetValue(param, out Control? value))
		{
			return value;
		}

		if (param is FileModelDto file
			&& file.IsEdited
			&& CreateEditingFileControl(file, out Control? control))
		{
			_cache.Add(param, control);

			return control;
		}

		return GetPlugControl(param?.GetType().Name);
	}

	/// <inheritdoc />
	public bool Match(object? data) => data is FileModelDto;

	/// <summary>
	/// Returns default control plug.
	/// </summary>
	internal static Control GetPlugControl(string? typeName) => new TextBlock
	{
		FontSize = 24.0,
		Foreground = Brushes.OrangeRed,
		HorizontalAlignment = HorizontalAlignment.Center,
		Text = "Not found view for: " + typeName,
		VerticalAlignment = VerticalAlignment.Center
	};
	#endregion

	#region Service
	/// <summary>
	/// Creates a control for editing a file.
	/// </summary>
	private static bool CreateEditingFileControl(
		FileModelDto file,
		[NotNullWhen(true)] out Control? control)
	{
		control = null;

		if (file.EntityType == EntityType.File)
		{
			EmbeddedFileEditorView view = _viewFactory.CreateUserControl<EmbeddedFileEditorView>();

			Initialize(view.ViewModel);

			control = view;

			return true;
		}
		else if (file.EntityType == EntityType.DataSet)
		{
			DatasetEditorView view = _viewFactory.CreateUserControl<DatasetEditorView>();

			Initialize(view.ViewModel);

			control = view;

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
