using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using Material.Icons.Avalonia;
using Shared.Common;
using Shared.Extensions;
using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;
using System.Windows.Input;
using BrushExtensions = DataOrganizer.Extensions.BrushExtensions;

namespace DataOrganizer.Views;

internal sealed partial class ClipboardTextBlock : UserControl
{
	#region Properties
	/// <summary>
	/// Text output area color, used for color animation.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Brush? AreaBrush
	{
		get => GetValue(AreaBrushProperty);
		set => SetValue(AreaBrushProperty, value);
	}

	/// <summary>
	/// Text blur radius.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public double BlurRadius
	{
		get => GetValue(BlurRadiusProperty);
		set => SetValue(BlurRadiusProperty, value);
	}

	/// <summary>
	/// Color sample.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Brush? ColorSampleBrush
	{
		get => GetValue(ColorSampleBrushProperty);
		set => SetValue(ColorSampleBrushProperty, value);
	}

	/// <summary>
	/// Returns <c>True</c> if value in <see cref="Text" /> is color.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool IsColor
	{
		get => GetValue(IsColorProperty);
		set => SetValue(IsColorProperty, value);
	}

	/// <summary>
	/// Returns <c>True</c> when <see cref="Text" /> value should be hidden.
	/// </summary>
	public bool IsHidden
	{
		get => GetValue(IsHiddenProperty);
		set => SetValue(IsHiddenProperty, value);
	}

	/// <summary>
	/// Command to detect when the user has manually changed <see cref="IsHidden" />.
	/// </summary>
	public ICommand? IsHiddenChangedCommand
	{
		get => GetValue(IsHiddenChangedCommandProperty);
		set => SetValue(IsHiddenChangedCommandProperty, value);
	}

	/// <summary>
	/// Returns <c>True</c> if user can hide <see cref="Text" />.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool IsHideEnabled
	{
		get => GetValue(IsHideEnabledProperty);
		set => SetValue(IsHideEnabledProperty, value);
	}

	/// <summary>
	/// Used to enable color animation of <see cref="AreaBrush" />.
	/// </summary>
	public bool IsHighlight
	{
		get => GetValue(IsHighlightProperty);
		set => SetValue(IsHighlightProperty, value);
	}

	/// <summary>
	/// Returns <c>True</c> if value in <see cref="Text" /> is hyperlink.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool IsHyperlink
	{
		get => GetValue(IsHyperlinkProperty);
		set => SetValue(IsHyperlinkProperty, value);
	}

	/// <summary>
	/// Returns <c>True</c> if the visual control for <see cref="Note" /> should be visible.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool IsNoteVisible
	{
		get => GetValue(IsNoteVisibleProperty);
		set => SetValue(IsNoteVisibleProperty, value);
	}

	/// <summary>
	/// Note.
	/// </summary>
	public string? Note
	{
		get => GetValue(NoteProperty);
		set => SetValue(NoteProperty, value);
	}

	/// <summary>
	/// Controls the display of popup for note.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool ShowNote
	{
		get => GetValue(ShowNoteProperty);
		set => SetValue(ShowNoteProperty, value);
	}

	/// <summary>
	/// Text.
	/// </summary>
	public string? Text
	{
		get => GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	/// <summary>
	/// <see cref="InputElement.DoubleTapped" /> event handler of <see cref="TextBlock" />.
	/// </summary>
	public ICommand? TextBlockDoubleTappedCommand
	{
		get => GetValue(TextBlockDoubleTappedCommandProperty);
		set => SetValue(TextBlockDoubleTappedCommandProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="AreaBrush" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<Brush?> AreaBrushProperty = AvaloniaProperty
		.Register<ClipboardTextBlock, Brush?>(name: nameof(AreaBrush));

	/// <summary>
	/// Identifies the <see cref="BlurRadius" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<double> BlurRadiusProperty = AvaloniaProperty
		.Register<ClipboardTextBlock, double>(name: nameof(BlurRadius));

	/// <summary>
	/// Identifies the <see cref="ColorSampleBrush" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<Brush?> ColorSampleBrushProperty = AvaloniaProperty
		.Register<ClipboardTextBlock, Brush?>(name: nameof(ColorSampleBrush));

	/// <summary>
	/// Identifies the <see cref="IsColor" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsColorProperty = AvaloniaProperty
		.Register<ClipboardTextBlock, bool>(name: nameof(IsColor));

	/// <summary>
	/// Identifies the <see cref="IsHiddenChangedCommand" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<ICommand?> IsHiddenChangedCommandProperty = AvaloniaProperty
		.Register<ClipboardTextBlock, ICommand?>(name: nameof(IsHiddenChangedCommand));

	/// <summary>
	/// Identifies the <see cref="IsHidden" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsHiddenProperty = AvaloniaProperty
		.Register<ClipboardTextBlock, bool>(name: nameof(IsHidden));

	/// <summary>
	/// Identifies the <see cref="IsHideEnabled" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsHideEnabledProperty = AvaloniaProperty
		.Register<ClipboardTextBlock, bool>(name: nameof(IsHideEnabled));

	/// <summary>
	/// Identifies the <see cref="IsHighlight" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsHighlightProperty = AvaloniaProperty
		.Register<ClipboardTextBlock, bool>(name: nameof(IsHighlight));

	/// <summary>
	/// Identifies the <see cref="IsHyperlink" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsHyperlinkProperty = AvaloniaProperty
		.Register<ClipboardTextBlock, bool>(name: nameof(IsHyperlink));

	/// <summary>
	/// Identifies the <see cref="IsNoteVisible" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsNoteVisibleProperty = AvaloniaProperty
		.Register<ClipboardTextBlock, bool>(name: nameof(IsNoteVisible));

	/// <summary>
	/// Identifies the <see cref="Note" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> NoteProperty = AvaloniaProperty
		.Register<ClipboardTextBlock, string?>(name: nameof(Note));

	/// <summary>
	/// Identifies the <see cref="ShowNote" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> ShowNoteProperty = AvaloniaProperty
		.Register<ClipboardTextBlock, bool>(name: nameof(ShowNote));

	/// <summary>
	/// Identifies the <see cref="TextBlockDoubleTappedCommand" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<ICommand?> TextBlockDoubleTappedCommandProperty = AvaloniaProperty
		.Register<ClipboardTextBlock, ICommand?>(name: nameof(TextBlockDoubleTappedCommand));

	/// <summary>
	/// Identifies the <see cref="Text" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty
		.Register<ClipboardTextBlock, string?>(name: nameof(Text));
	#endregion

	#region Auto-Generated Commands
	/// <summary>
	/// Copies <see cref="Text" /> value to system clipboard.
	/// </summary>
	[RelayCommand(CanExecute = nameof(IsTextNotNull))]
	private async Task CopyToClipboard()
	{
		if (string.IsNullOrWhiteSpace(Text) || TopLevel
			.GetTopLevel(this)?
			.Clipboard is not { } clipboard)
		{
			return;
		}

		try
		{
			await clipboard
				.SetTextAsync(Text)
				.ConfigureAwait(true);
		}
		finally
		{
			IsHighlight = true;

			IsHighlight = false;
		}
	}

	/// <summary>
	/// <see cref="InputElement.PointerEntered" /> event handler of control for "Note".
	/// </summary>
	[RelayCommand]
	private async Task NotePointerEntered(MaterialIcon? icon)
	{
		if (icon is null)
		{
			return;
		}

		await Task
			.Delay(AppUtils.TipDelay)
			.ConfigureAwait(true);

		if (!icon.IsPointerOver)
		{
			return;
		}

		ShowNote = true;
	}
	#endregion

	#region Data
	/// <inheritdoc cref="CompositeDisposable" />
	private readonly CompositeDisposable _disposables = [];
	#endregion

	#region Constructors
	public ClipboardTextBlock()
	{
		InitializeComponent();
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="IsHiddenProperty" /> changed handler.
	/// </summary>
	private void IsHiddenProperty_Changed(bool value)
	{
		BlurRadius = value ? 20.0 : default;

		SetColorSampleBrush(Text);
	}

	/// <summary>
	/// <see cref="IsHighlightProperty" /> changed handler.
	/// </summary>
	private void IsHighlightProperty_Changed(bool value)
	{
		if (!value)
		{
			return;
		}

		_ = BrushExtensions.ApplyLimeGreenColorAnimation(() => AreaBrush);
	}

	/// <summary>
	/// <see cref="NoteProperty" /> changed handler.
	/// </summary>
	private void NoteProperty_Changed(string? value) => IsNoteVisible = !string.IsNullOrWhiteSpace(value);

	/// <summary>
	/// <see cref="TextProperty" /> changed handler.
	/// </summary>
	private void TextProperty_Changed(string? value)
	{
		CopyToClipboardCommand.NotifyCanExecuteChanged();

		IsHyperlink = value.IsUriFormat();

		IsColor = value.IsHtmlColorFormat();

		SetColorSampleBrush(value);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);

		IsHideEnabled = BindingOperations.GetBindingExpressionBase(this, IsHiddenProperty) is not null;

		this
			.GetObservable(IsHiddenProperty)
			.Subscribe(IsHiddenProperty_Changed)
			.DisposeWith(_disposables);

		this
			.GetObservable(IsHighlightProperty)
			.Subscribe(IsHighlightProperty_Changed)
			.DisposeWith(_disposables);

		this
			.GetObservable(NoteProperty)
			.Subscribe(NoteProperty_Changed)
			.DisposeWith(_disposables);

		this
			.GetObservable(TextProperty)
			.Subscribe(TextProperty_Changed)
			.DisposeWith(_disposables);
	}

	/// <inheritdoc />
	protected override void OnUnloaded(RoutedEventArgs e)
	{
		base.OnUnloaded(e);

		// Disposes current subscriptions but keeps the container alive so
		// the next OnLoaded can re-subscribe into the same _disposables.
		_disposables.Clear();
	}
	#endregion

	#region Service
	/// <summary>
	/// Returns <c>True</c> if <see cref="Text" /> is not null.
	/// </summary>
	private bool IsTextNotNull() => !string.IsNullOrWhiteSpace(Text);

	/// <summary>
	/// Sets <see cref="ColorSampleBrush" /> from string value.
	/// </summary>
	private void SetColorSampleBrush(string? text)
	{
		ColorSampleBrush = IsColor && text is not null
			? SolidColorBrush.Parse(text)
			: null;
	}
	#endregion
}
