using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Abstract;
using DataOrganizer.DTO;
using DataOrganizer.Extensions;
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
using System.Threading.Channels;
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

		bool initialIsReadOnly = IsReadOnly;

		// At the time of initialization, prohibit making changes.
		editor.IsReadOnly = true;

		_editor = editor;

		ContentsIsValidPair result = await _dbAccess
			.GetFileContentsAsync(FileId)
			.ConfigureAwait(true);

		try
		{
			if (!result.IsValid || TryToDecrypt(result.Contents) is not { } output)
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

			try
			{
				TextEditorHelper.SubscribePointerWheelChanged(
					editor,
					() => FontSize,
					() => FontSize);

				ApplyEditorSettings(editor);

				await InitializePropertiesAsync(editor).ConfigureAwait(true);

				TimeSpan delay = TimeSpan.FromSeconds(0.5);

				Observable.FromEventPattern<EventHandler, EventArgs>(
					x => editor.TextChanged += x,
					x => editor.TextChanged -= x)
					.SetDelay(delay, false)
					.Subscribe(Editor_TextChanged)
					.DisposeWith(_disposables);

				Observable.FromEventPattern<EventHandler, EventArgs>(
					x => editor.TextArea.Caret.PositionChanged += x,
					x => editor.TextArea.Caret.PositionChanged -= x)
					.SetDelay(delay, false)
					.Subscribe(Editor_PropertyChanged)
					.DisposeWith(_disposables);

				Observable.FromEventPattern<EventHandler, EventArgs>(
					x => editor.TextArea.SelectionChanged += x,
					x => editor.TextArea.SelectionChanged -= x)
					.SetDelay(delay, false)
					.Subscribe(Editor_PropertyChanged)
					.DisposeWith(_disposables);

				//// ScrollToVerticalOffset() and ScrollToHorizontalOffset() are not implemented in TextEditor.
				//Observable.FromEventPattern<EventHandler, EventArgs>(
				//	x => editor.TextArea.TextView.ScrollOffsetChanged += x,
				//	x => editor.TextArea.TextView.ScrollOffsetChanged -= x)
				//	.SetDelay(delay, false)
				//	.Subscribe(Editor_PropertyChanged)
				//	.DisposeWith(_disposables);

				_ = Task.Run(() => ProcessSaveChannelAsync());

				_logger.LogInformation($@"Content is initialized in ""{GetType().Name}""");

				await Task
					.Delay(100)
					.ConfigureAwait(true);

				editor.Focus();
			}
			finally
			{
				output.ZeroMemory();
			}
		}
		finally
		{
			editor.IsReadOnly = initialIsReadOnly;

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
	/// Channel for save operations.
	/// </summary>
	private readonly Channel<byte[]> _saveChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
	{
		SingleReader = true,
		SingleWriter = true
	});

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
		ILogger logger) : base(
			app,
			dbAccess,
			dispatcher,
			entityEcryption,
			jsonSerializer,
			logger)
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

			_ = TrySavePropertiesAsync();
		}
	}

	/// <summary>
	/// <see cref="TextEditor.TextChanged" /> event handler.
	/// </summary>
	private void Editor_TextChanged(EventPattern<EventArgs> e)
	{
		if (IsContentCorrupted
			|| IsReadOnly
			|| e.Sender is not TextEditor editor)
		{
			return;
		}

		_saveChannel.Writer.TryWrite(TextHelper
			.Utf8Encoding
			.GetBytes(editor.Text));
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void AfterDispose()
	{
		if (!_saveChannel
			.Reader
			.Completion
			.IsCompleted)
		{
			_saveChannel
				.Writer
				.Complete();
		}

		base.AfterDispose();
	}

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

			_ = TrySavePropertiesAsync();
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

	/// <summary>
	/// Background consumer that processes the save channel sequentially.
	/// Drains all queued items and saves only the latest.
	/// </summary>
	private async Task ProcessSaveChannelAsync()
	{
		ChannelReader<byte[]> reader = _saveChannel.Reader;

		await foreach (byte[] contents in reader
			.ReadAllAsync()
			.ConfigureAwait(false))
		{
			// Drain the channel — keep only the latest, ZeroMemory the rest.
			byte[] latest = contents;

			// Insurance in case of:
			// - slow encryption (large file)
			// - slow DB (disk under load)
			// - quick paste (Ctrl+V of large text can cause several TextChanged in a row)
			while (reader.TryRead(out byte[]? newer))
			{
				latest.ZeroMemory();

				latest = newer;
			}

			if (TryToEncrypt(latest) is not { } output)
			{
				ShowErrorSnackbar(_editor, Strings.FailedToProcessContents);

				latest.ZeroMemory();

				continue;
			}

			try
			{
				await SaveContentsAsync(output).ConfigureAwait(false);
			}
			finally
			{
				latest.ZeroMemory();

				output.ZeroMemory();
			}
		}
	}

	/// <summary>
	/// Tries to save properties.
	/// </summary>
	private Task TrySavePropertiesAsync(CancellationToken token = default)
	{
		string json = _jsonSerializer.Serialize(CreateProperties(), AppUtils.JsonOptions);

		SetPropertiesCallback?.Invoke(json);

		if (IsReadOnly)
		{
			return Task.CompletedTask;
		}

		return SavePropertiesAsync(json, token);
	}
	#endregion
}
