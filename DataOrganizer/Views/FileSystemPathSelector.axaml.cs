using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;

namespace DataOrganizer.Views;

public sealed partial class FileSystemPathSelector : UserControl
{
	#region Properties
	/// <summary>
	/// A path to file system entry.
	/// </summary>
	public string? Path
	{
		get => GetValue(PathProperty);
		set => SetValue(PathProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="Path" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> PathProperty = AvaloniaProperty
		.Register<FileSystemPathSelector, string?>(name: nameof(Path));
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Clears value in <see cref="Path" />.
	/// </summary>
	[RelayCommand(CanExecute = nameof(CanClear))]
	private void Clear() => Path = null;

	/// <summary>
	/// Selects a path to file system entry.
	/// </summary>
	[RelayCommand]
	private async Task Select()
	{
		if (TopLevel
			.GetTopLevel(this)?
			.StorageProvider is not { } provider)
		{
			return;
		}

		FilePickerOpenOptions options = new()
		{
			AllowMultiple = false,
			SuggestedFileType = FilePickerFileTypes.All
		};

		IReadOnlyList<IStorageFile> selected = await provider
			.OpenFilePickerAsync(options)
			.ConfigureAwait(true);

		if (selected.Count == 0)
		{
			return;
		}

		using IStorageFile file = selected[0];

		Path = file
			.Path
			.LocalPath;
	}
	#endregion

	#region Data
	/// <inheritdoc cref="CompositeDisposable" />
	private readonly CompositeDisposable _disposables = [];
	#endregion

	#region Constructors
	public FileSystemPathSelector()
	{
		InitializeComponent();

		this
			.GetObservable(PathProperty)
			.Subscribe(PathProperty_Changed)
			.DisposeWith(_disposables);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="PathProperty" /> changed handler.
	/// </summary>
	private void PathProperty_Changed(string? value) => ClearCommand.NotifyCanExecuteChanged();
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromLogicalTree(e);

		_disposables.Dispose();
	}
	#endregion

	#region Service
	/// <summary>
	/// Validates <see cref="ClearCommand" />.
	/// </summary>
	private bool CanClear() => !string.IsNullOrWhiteSpace(Path);
	#endregion
}