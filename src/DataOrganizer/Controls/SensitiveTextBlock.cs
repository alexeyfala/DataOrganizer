using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using System;

namespace DataOrganizer.Controls;

/// <summary>
/// A text block that gives UI Automation a neutral word instead of its text while the text is sensitive.
/// </summary>
internal sealed class SensitiveTextBlock : TextBlock
{
	#region Properties
	/// <summary>
	/// <c>True</c> while the text is sensitive.
	/// </summary>
	public bool IsSensitive
	{
		get => GetValue(IsSensitiveProperty);
		set => SetValue(IsSensitiveProperty, value);
	}

	/// <inheritdoc />
	protected override Type StyleKeyOverride { get; } = typeof(TextBlock);
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override AutomationPeer OnCreateAutomationPeer() => new SensitiveTextBlockAutomationPeer(this);
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="IsSensitive" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsSensitiveProperty = AvaloniaProperty
		.Register<SensitiveTextBlock, bool>(name: nameof(IsSensitive));
	#endregion
}
