using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.DTO;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="EmbeddedFileEditorView" />.
/// </summary>
public sealed partial class EmbeddedFileEditorViewModel : EmbeddedEditorViewModelBase
{
	#region Auto-Generated Properties
	/// <inheritdoc cref="FileProperties.FontSize" />
	[ObservableProperty]
	private double _fontSize = 14.0;

	/// <inheritdoc cref="FileProperties.IsWordWrap" />
	[ObservableProperty]
	private bool _isWordWrap;
	#endregion

	#region Commands
	/// <inheritdoc cref="TextEditorHelper.Copy" />
	public RelayCommand<TextArea> CopyCommand { get; } = new(TextEditorHelper.Copy, TextEditorHelper.CanExecuteCopy);

	/// <inheritdoc cref="TextEditorHelper.Find" />
	public RelayCommand<TextArea> FindCommand { get; } = new(TextEditorHelper.Find);

	/// <inheritdoc cref="TextEditorHelper.ScrollToEnd" />
	public RelayCommand<TextEditor> ScrollToEndCommand { get; } = new(TextEditorHelper.ScrollToEnd);

	/// <inheritdoc cref="TextEditorHelper.ScrollToTop" />
	public RelayCommand<TextEditor> ScrollToTopCommand { get; } = new(TextEditorHelper.ScrollToTop);

	/// <inheritdoc cref="TextEditorHelper.SelectAll" />
	public RelayCommand<TextEditor> SelectAllCommand { get; } = new(TextEditorHelper.SelectAll, TextEditorHelper.CanExecuteSelectAll);

	/// <inheritdoc cref="TextEditorHelper.Spin" />
	public RelayCommand<SpinEventArgs> SpinCommand { get; }
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Handles the <see cref="Control.Loaded" /> event of <see cref="TextEditor" />.
	/// </summary>
	[RelayCommand]
	public async Task EditorLoaded(TextEditor? editor)
	{
		if (IsInitialized || editor is null)
		{
			return;
		}

		_editor = editor;

		ContentsIsValidPair result = await _dbAccess
			.GetFileContentsAsync(FileId)
			.ConfigureAwait(false);

		try
		{
			if (!result.IsValid || !TryToDecrypt(result.Contents, out byte[] output))
			{
				IsContentCorrupted = true;

				ShowErrorSnackbar(editor, Strings.FailedToProcessContents);

				_logger.LogError(
					$@"{Strings.FailedToLoadFileContents} of file ""{FileId}""",
					isAssertDebug: false);

				return;
			}

			editor.Text = TextHelper
				.Utf8Encoding
				.GetString(output);

			TextEditorHelper.SubscribePointerWheelChanged(
				editor,
				() => FontSize,
				() => FontSize);

			ApplyEditorSettings(editor);

			await InitializePropertiesAsync(editor).ConfigureAwait(false);

			Observable.FromEventPattern<EventHandler, EventArgs>(
				x => editor.TextChanged += x,
				x => editor.TextChanged -= x)
				.Subscribe(Editor_TextChanged)
				.DisposeWith(_disposables);

			Observable.FromEventPattern<EventHandler, EventArgs>(
				x => editor.TextArea.Caret.PositionChanged += x,
				x => editor.TextArea.Caret.PositionChanged -= x)
				.Subscribe(Editor_PropertyChanged)
				.DisposeWith(_disposables);

			Observable.FromEventPattern<EventHandler, EventArgs>(
				x => editor.TextArea.SelectionChanged += x,
				x => editor.TextArea.SelectionChanged -= x)
				.Subscribe(Editor_PropertyChanged)
				.DisposeWith(_disposables);

			//// ScrollToVerticalOffset() and ScrollToHorizontalOffset() are not implemented in TextEditor.
			//Observable.FromEventPattern<EventHandler, EventArgs>(
			//	x => editor.TextArea.TextView.ScrollOffsetChanged += x,
			//	x => editor.TextArea.TextView.ScrollOffsetChanged -= x)
			//	.Subscribe(Editor_PropertyChanged)
			//	.DisposeWith(_disposables);

			_logger.LogInformation($@"Content is initialized in ""{GetType().Name}""");

			await Task
				.Delay(100)
				.ConfigureAwait(true);

			editor.Focus();
		}
		finally
		{
			IsInitialized = true;
		}
	}

	/// <summary>
	/// Handles the <see cref="Visual.DetachedFromVisualTree" /> event of <see cref="TextEditor" />.
	/// </summary>
	[RelayCommand]
	private void DetachedFromVisualTree(TextEditor? editor)
	{
		if (editor is null)
		{
			return;
		}

		TextEditorHelper.UnsubscribePointerWheelChanged(
			editor,
			() => FontSize,
			() => FontSize);
	}

	/// <inheritdoc cref="EmbeddedEditorViewModelBase.ShowInListAsync" />
	[RelayCommand]
	private void ShowInList(Window? window) => _ = ShowInListAsync(window, FileId);
	#endregion

	#region Data
	/// <summary>
	/// Reference to <see cref="TextEditor" />.
	/// </summary>
	private TextEditor? _editor;
	#endregion

	#region Constructors
	public EmbeddedFileEditorViewModel(
		Application app,
		IDbAccess dbAccess,
		IDispatcher dispatcher,
		IEntityEcryption entityEcryption,
		IJsonSerializerWrapper jsonSerializer,
		ILogger logger) : base(app, dbAccess, dispatcher, entityEcryption, jsonSerializer, logger)
	{
		SpinCommand = new(e => TextEditorHelper.Spin(e, FontSize, () => FontSize));
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// Event handler of <see cref="TextEditor" />.
	/// </summary>
	private void Editor_PropertyChanged(EventPattern<EventArgs> e)
	{
		lock (_mutex)
		{
			if (IsContentCorrupted)
			{
				return;
			}

			string json = _jsonSerializer.Serialize(CreateProperties(), AppUtils.JsonOptions);

			SetPropertiesCallback?.Invoke(json);

			if (IsReadOnly)
			{
				return;
			}

			_ = SavePropertiesAsync(json);
		}
	}

	/// <summary>
	/// <see cref="TextEditor.TextChanged" /> event handler.
	/// </summary>
	private void Editor_TextChanged(EventPattern<EventArgs> e)
	{
		lock (_mutex)
		{
			if (IsContentCorrupted
				|| IsReadOnly
				|| e.Sender is not TextEditor editor)
			{
				return;
			}

			byte[] contents = TextHelper
				.Utf8Encoding
				.GetBytes(editor.Text);

			// Encryption slows down the UI, so it is performed in a different thread.
			_ = Task.Run(() =>
			{
				if (!TryToEncrypt(contents, out byte[] output))
				{
					ShowErrorSnackbar(editor, Strings.FailedToProcessContents);

					return Task.CompletedTask;
				}

				return SaveContentsAsync(output);
			});
		}
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		lock (_mutex)
		{
			if (IsContentCorrupted || !IsInitialized)
			{
				return;
			}

			if (!string.Equals(e.PropertyName, nameof(FontSize)) && !string.Equals(e.PropertyName, nameof(IsWordWrap)))
			{
				return;
			}

			string json = _jsonSerializer.Serialize(CreateProperties(), AppUtils.JsonOptions);

			SetPropertiesCallback?.Invoke(json);

			if (IsReadOnly)
			{
				return;
			}

			_ = SavePropertiesAsync(json);
		}
	}
	#endregion

	#region Service
	/// <summary>
	/// Applies settings to <see cref="TextEditor" />.
	/// </summary>
	private static void ApplyEditorSettings(TextEditor editor)
	{
		editor
			.Options
			.HighlightCurrentLine = true;

		editor
			.Options
			.EnableEmailHyperlinks = false;

		editor
			.Options
			.AllowScrollBelowDocument = false;
	}

	/// <summary>
	/// Creates <see cref="FileProperties" /> from <see cref="EmbeddedFileEditorViewModel" /> and <see cref="TextEditor" /> properties.
	/// </summary>
	private FileProperties CreateProperties()
	{
		if (_editor is not { } editor)
		{
			return default;
		}

		return new()
		{
			CaretPosition = editor.TextArea.Caret.Position,
			FontSize = FontSize,
			IsWordWrap = IsWordWrap,
			ScrollOffset = new((int)editor.HorizontalOffset, (int)editor.VerticalOffset),
			SelectionLength = editor.SelectionLength,
			SelectionStart = editor.SelectionStart
		};
	}

	/// <summary>
	/// Initializes <see cref="EmbeddedFileEditorViewModel" /> properties from database.
	/// </summary>
	private async Task InitializePropertiesAsync(TextEditor editor, CancellationToken token = default)
	{
		string? value = InitialProperties ?? await _dbAccess
			.GetFilePropertiesAsync(FileId, token)
			.ConfigureAwait(false);

		if (value is null)
		{
			return;
		}

		try
		{
			FileProperties properties = _jsonSerializer.Deserialize<FileProperties>(value);

			FontSize = properties.FontSize;

			IsWordWrap = properties.IsWordWrap;

			editor.SelectionStart = properties.SelectionStart;

			editor.SelectionLength = properties.SelectionLength;

			// Not implemented in TextEditor.
			editor.ScrollToVerticalOffset(properties.ScrollOffset.Y);

			// Not implemented in TextEditor.
			editor.ScrollToHorizontalOffset(properties.ScrollOffset.X);

			editor.ScrollToLine(properties.CaretPosition.Line);

			editor
				.TextArea
				.Caret
				.Position = properties.CaretPosition;

			_logger.LogDebug(
				$@"Properties ""{FileId}"" for editor are initialized:{properties.GetPropertyValues(true)}");
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, isAssertDebug: false);

			if (!IsReadOnly)
			{
				await SavePropertiesAsync(
					_jsonSerializer.Serialize(CreateProperties(), AppUtils.JsonOptions),
					token);
			}
		}
	}
	#endregion
}
