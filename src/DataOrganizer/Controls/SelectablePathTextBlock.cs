using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Logging;
using System;

namespace DataOrganizer.Controls;

/// <summary>
/// A selectable path that wraps after its separators instead of inside its names, and copies as it is.
/// </summary>
internal sealed class SelectablePathTextBlock : SelectableTextBlock
{
	#region Properties
	/// <summary>
	/// The path shown.
	/// </summary>
	public string? Path
	{
		get => GetValue(PathProperty);
		set => SetValue(PathProperty, value);
	}

	/// <inheritdoc />
	protected override Type StyleKeyOverride { get; } = typeof(SelectableTextBlock);
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="Path" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> PathProperty = AvaloniaProperty
		.Register<SelectablePathTextBlock, string?>(name: nameof(Path));
	#endregion

	#region Data
	/// <summary>
	/// Invisible zero-width space, where a line may break.
	/// </summary>
	private const string BreakMark = "​";
	#endregion

	#region Constructors
	public SelectablePathTextBlock() => AddHandler(CopyingToClipboardEvent, SelectablePathTextBlock_CopyingToClipboard);
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="SelectableTextBlock.CopyingToClipboardEvent" /> handler of the control.
	/// </summary>
	private async void SelectablePathTextBlock_CopyingToClipboard(object? sender, RoutedEventArgs e)
	{
		// The built-in copy would take the marks along, and a pasted path with them leads nowhere.
		e.Handled = true;

		string path = SelectedText.Replace(
			BreakMark,
			string.Empty,
			StringComparison.Ordinal);

		if (TopLevel
			.GetTopLevel(this)?
			.Clipboard is not { } clipboard)
		{
			return;
		}

		try
		{
			await clipboard
				.SetTextAsync(path)
				.ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			// As in the built-in copy, a clipboard that refuses the text is only logged.
			Logger
				.TryGet(LogEventLevel.Warning, LogArea.Control)?
				.Log(this, "Failed to write the path to the clipboard: {Error}", ex);
		}
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override AutomationPeer OnCreateAutomationPeer() => new SelectablePathTextBlockAutomationPeer(this);

	/// <inheritdoc />
	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property != PathProperty)
		{
			return;
		}

		// Text breaks only at spaces and a few signs, so a path without spaces breaks after its drive and inside a
		// name.
		Text = Path?
			.Replace("\\", "\\" + BreakMark, StringComparison.Ordinal)
			.Replace("/", "/" + BreakMark, StringComparison.Ordinal);
	}
	#endregion
}
