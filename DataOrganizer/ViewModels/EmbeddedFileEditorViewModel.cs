using Avalonia;
using Avalonia.Controls;
using AvaloniaEdit;
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
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.ViewModels;

/// <summary>
/// View model for <see cref="EmbeddedFileEditorView" />.
/// </summary>
public sealed partial class EmbeddedFileEditorViewModel : TextEditorViewModelBase, IFileEditor
{
	#region Properties
	/// <inheritdoc />
	public byte[]? EncryptedPassword { get; set; }

	/// <inheritdoc />
	public Guid FileId { get; set; }

	/// <inheritdoc />
	public string? InitialProperties { get; set; }

	/// <inheritdoc />
	public bool IsInitialized { get; private set; }

	/// <inheritdoc />
	public Action<string>? SetPropertiesCallback { get; set; }

	/// <inheritdoc />
	public Action<DateTime>? SetUpdatedDateCallback { get; set; }
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Handles the <see cref="Control.Loaded" /> event of <see cref="TextEditor" />.
	/// </summary>
	[RelayCommand]
	public async Task EditorLoaded(TextEditor editor)
	{
		if (IsInitialized || editor is null)
		{
			return;
		}

		_editor = editor;

		SubscribePointerWheelChanged(editor);

		ContentsIsValidPair result = await _dbAccess
			.GetFileContentsAsync(FileId)
			.ConfigureAwait(false);

		if (result.IsDefault() || !result.IsValid)
		{
			_logger.LogError($@"{Strings.FailedToLoadFileContents} of file ""{FileId}""");

			return;
		}

		byte[] contents = result.Contents;

		if (EncryptedPassword?.Length > 0 && !DecryptContents(
			contents,
			EncryptedPassword,
			out contents))
		{
			ShowErrorSnackbar(
				editor.FindLogicalParent<Window>(),
				Strings.FailedToProcessContents);

			return;
		}

		editor.Text = TextHelper
			.Utf8Encoding
			.GetString(contents);

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

		IsInitialized = true;

		await Task
			.Delay(100)
			.ConfigureAwait(true);

		editor.Focus();
	}

	/// <inheritdoc cref="EditorViewModelBase.ShowInListAsync" />
	[RelayCommand]
	private void ShowInList(Window? window) => _ = ShowInListAsync(window, FileId);
	#endregion

	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IEncryptionService" />
	private readonly IEncryptionService _encryption;

	/// <inheritdoc cref="IEntityEcryption" />
	private readonly IEntityEcryption _entityEcryption;

	/// <inheritdoc cref="IJsonSerializerWrapper" />
	private readonly IJsonSerializerWrapper _jsonSerializer;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <summary>
	/// Reference to <see cref="TextEditor" />.
	/// </summary>
	private TextEditor? _editor;
	#endregion

	#region Constructors
	public EmbeddedFileEditorViewModel(
		Application app,
		IDbAccess dbAccess,
		IEncryptionService encryption,
		IEntityEcryption entityEcryption,
		IJsonSerializerWrapper jsonSerializer,
		ILogger logger) : base(app)
	{
		_dbAccess = dbAccess;

		_encryption = encryption;

		_entityEcryption = entityEcryption;

		_jsonSerializer = jsonSerializer;

		_logger = logger;
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
			string json = _jsonSerializer.Serialize(CreateProperties(), AppUtils.JsonOptions);

			SetPropertiesCallback?.Invoke(json);

			if (IsReadOnly)
			{
				return;
			}

			_ = this.SavePropertiesAsync(_dbAccess, _logger, json);
		}
	}

	/// <summary>
	/// <see cref="TextEditor.TextChanged" /> event handler.
	/// </summary>
	private void Editor_TextChanged(EventPattern<EventArgs> e)
	{
		lock (_mutex)
		{
			if (IsReadOnly || e.Sender is not TextEditor editor)
			{
				return;
			}

			byte[] contents = TextHelper
				.Utf8Encoding
				.GetBytes(editor.Text);

			if (EncryptedPassword?.Length > 0 && !EncryptContents(
				contents,
				EncryptedPassword,
				out contents))
			{
				ShowErrorSnackbar(
					editor.FindLogicalParent<Window>(),
					Strings.FailedToProcessContents);

				return;
			}

			_ = this.SaveContentsAsync(
				_dbAccess,
				_logger,
				contents);
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
			if (!IsInitialized)
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

			_ = this.SavePropertiesAsync(_dbAccess, _logger, json);
		}
	}
	#endregion

	#region Service
	/// <summary>
	/// Applies settings to <see cref="TextEditor" />.
	/// </summary>
	private static void ApplyEditorSettings(TextEditor editor)
	{
		editor.Options.HighlightCurrentLine = true;

		editor.Options.EnableEmailHyperlinks = false;

		editor.Options.AllowScrollBelowDocument = false;
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
	/// Decrypts file contents.
	/// </summary>
	private bool DecryptContents(
		byte[] input,
		byte[] encryptedPassword,
		out byte[] output)
	{
		output = [];

		if (!_encryption.Decrypt(
			encryptedPassword,
			_entityEcryption.GetSessionId(),
			out byte[] decryptedPassword))
		{
			return false;
		}

		try
		{
			if (!_encryption.Decrypt(
				input,
				decryptedPassword,
				out byte[] decryptedContents))
			{
				return false;
			}

			output = decryptedContents;

			return true;
		}
		finally
		{
			CryptographicOperations.ZeroMemory(decryptedPassword);
		}
	}

	/// <summary>
	/// Encrypts file contents.
	/// </summary>
	private bool EncryptContents(
		byte[] input,
		byte[] encryptedPassword,
		out byte[] output)
	{
		output = [];

		if (!_encryption.Decrypt(
			encryptedPassword,
			_entityEcryption.GetSessionId(),
			out byte[] decryptedPassword))
		{
			return false;
		}

		try
		{
			if (!_encryption.Encrypt(
				input,
				decryptedPassword,
				out byte[] encryptedContents))
			{
				return false;
			}

			output = encryptedContents;

			return true;
		}
		finally
		{
			CryptographicOperations.ZeroMemory(decryptedPassword);
		}
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
			_logger.LogException(ex, false);

			if (!IsReadOnly)
			{
				await this.SavePropertiesAsync(
					_dbAccess,
					_logger,
					_jsonSerializer.Serialize(CreateProperties(), AppUtils.JsonOptions),
					token);
			}
		}
	}
	#endregion
}
