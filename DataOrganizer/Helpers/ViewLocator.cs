using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.DependencyInjection;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using Entities.Enums;
using System;
using System.Collections.Generic;

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
		if (param is FileModelDto dto && dto.IsEdited)
		{
			if (_cache.TryGetValue(dto, out Control? value))
			{
				return value;
			}

			if (dto.EntityType == EntityType.File)
			{
				EmbeddedFileEditorView view = _viewFactory.CreateUserControl<EmbeddedFileEditorView>();

				Initialize(view.ViewModel);

				_cache.Add(dto, view);

				return view;
			}
			else if (dto.EntityType == EntityType.DataSet)
			{
				DatasetEditorView view = _viewFactory.CreateUserControl<DatasetEditorView>();

				Initialize(view.ViewModel);

				_cache.Add(dto, view);

				return view;
			}

			void Initialize(IFileEditor editor)
			{
				if (dto.EncryptionStatus == Enums.EncryptionStatus.Decrypted)
				{
					editor.EncryptedPassword = dto
						.FindParent(x => x.EncryptedPassword is not null)?
						.EncryptedPassword;
				}

				editor.FileId = dto.Id;

				editor.SetPropertiesCallback = SetProperties;

				editor.SetUpdatedDateCallback = SetUpdatedDate;

				editor.InitialProperties = dto.Properties;

				editor.Initialize();
			}

			void SetProperties(string properties) => dto.Properties = properties;

			void SetUpdatedDate(DateTime updatedDate) => dto.UpdatedDate = updatedDate;
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
}
