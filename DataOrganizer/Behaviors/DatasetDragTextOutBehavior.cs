using Avalonia;
using Avalonia.Input;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Enables dragging the associated element's <see cref="Text" /> out to other applications
/// as plain text, bypassing the system clipboard.
/// </summary>
internal sealed class DatasetDragTextOutBehavior : DatasetDragBehaviorBase
{
	#region Properties
	/// <summary>
	/// Text carried by the drag operation.
	/// </summary>
	public string? Text
	{
		get => GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="Text" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty
		.Register<DatasetDragTextOutBehavior, string?>(name: nameof(Text));
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override DragDropEffects AllowedEffects => DragDropEffects.Copy;

	/// <inheritdoc />
	protected override bool CanStartDrag(PointerEventArgs e) =>
		// Shift+drag is reserved for internal record reordering; yield the gesture to it.
		!e.KeyModifiers.HasFlag(KeyModifiers.Shift) && !string.IsNullOrWhiteSpace(Text);

	/// <inheritdoc />
	protected override DataTransfer? CreateDataTransfer()
	{
		DataTransfer data = new();

		data.Add(DataTransferItem.CreateText(Text));

		return data;
	}
	#endregion
}
