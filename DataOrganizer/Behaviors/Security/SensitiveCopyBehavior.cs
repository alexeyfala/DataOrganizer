using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using DataOrganizer.Interfaces.Clipboard;

namespace DataOrganizer.Behaviors.Security;

/// <summary>
/// Writes the copy taken from the attached <see cref="TextBox" /> or <see cref="SelectableTextBlock" />
/// with the clipboard sensitivity markers instead of plain text.
/// </summary>
internal sealed class SensitiveCopyBehavior : Behavior<Control>
{
	#region Properties
	/// <summary>
	/// <c>True</c> while the text of the attached control is sensitive.
	/// </summary>
	public bool IsSensitive
	{
		get => GetValue(IsSensitiveProperty);
		set => SetValue(IsSensitiveProperty, value);
	}

	/// <inheritdoc cref="ISensitiveClipboardWriter" />
	public ISensitiveClipboardWriter? Writer
	{
		get => GetValue(WriterProperty);
		set => SetValue(WriterProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="IsSensitive" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsSensitiveProperty = AvaloniaProperty
		.Register<SensitiveCopyBehavior, bool>(name: nameof(IsSensitive));

	/// <summary>
	/// Identifies the <see cref="Writer" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<ISensitiveClipboardWriter?> WriterProperty = AvaloniaProperty
		.Register<SensitiveCopyBehavior, ISensitiveClipboardWriter?>(name: nameof(Writer));
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="TextBox.CopyingToClipboardEvent" /> and
	/// <see cref="SelectableTextBlock.CopyingToClipboardEvent" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_CopyingToClipboard(object? sender, RoutedEventArgs e)
	{
		TryWrite(GetSelectedText(sender), e);
	}

	/// <summary>
	/// <see cref="TextBox.CuttingToClipboardEvent" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_CuttingToClipboard(object? sender, RoutedEventArgs e)
	{
		if (sender is not TextBox textBox || !TryWrite(textBox.SelectedText, e))
		{
			return;
		}

		// Handling the event cancels the built-in cut, the removal of the selection included.
		textBox.SelectedText = string.Empty;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		switch (AssociatedObject)
		{
			case TextBox textBox:
				textBox.AddHandler(TextBox.CopyingToClipboardEvent, AssociatedObject_CopyingToClipboard);

				textBox.AddHandler(TextBox.CuttingToClipboardEvent, AssociatedObject_CuttingToClipboard);
				break;

			case SelectableTextBlock textBlock:
				textBlock.AddHandler(SelectableTextBlock.CopyingToClipboardEvent, AssociatedObject_CopyingToClipboard);
				break;
		}
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		switch (AssociatedObject)
		{
			case TextBox textBox:
				textBox.RemoveHandler(TextBox.CopyingToClipboardEvent, AssociatedObject_CopyingToClipboard);

				textBox.RemoveHandler(TextBox.CuttingToClipboardEvent, AssociatedObject_CuttingToClipboard);
				break;

			case SelectableTextBlock textBlock:
				textBlock.RemoveHandler(SelectableTextBlock.CopyingToClipboardEvent, AssociatedObject_CopyingToClipboard);
				break;
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Selection of the control the event came from.
	/// </summary>
	private static string? GetSelectedText(object? sender)
	{
		return sender switch
		{
			TextBox textBox => textBox.SelectedText,
			SelectableTextBlock textBlock => textBlock.SelectedText,
			_ => null
		};
	}

	/// <summary>
	/// Writes <paramref name="text" /> with the markers; <c>false</c> when the built-in copy stays in charge.
	/// </summary>
	private bool TryWrite(string? text, RoutedEventArgs e)
	{
		if (!IsSensitive
			|| Writer is not { } writer
			|| string.IsNullOrEmpty(text))
		{
			return false;
		}

		// Handling the event cancels the built-in write, which carries no markers.
		e.Handled = true;

		writer.Write(text);

		return true;
	}
	#endregion
}
