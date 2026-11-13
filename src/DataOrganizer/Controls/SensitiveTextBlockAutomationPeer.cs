using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using DataOrganizer.Shared.Properties;
using System;
using System.Reactive.Linq;

namespace DataOrganizer.Controls;

/// <summary>
/// Automation peer of a <see cref="SensitiveTextBlock" />: a text named by the text, or by a neutral word while the text is
/// sensitive.
/// </summary>
// Not a TextBlockAutomationPeer: that one raises every change of the text with the text itself.
internal sealed class SensitiveTextBlockAutomationPeer : ControlAutomationPeer
{
	#region Properties
	/// <summary>
	/// Text block of the peer.
	/// </summary>
	private SensitiveTextBlock Block => (SensitiveTextBlock)Owner;
	#endregion

	#region Constructors
	public SensitiveTextBlockAutomationPeer(SensitiveTextBlock owner)
		: base(owner)
	{
		// The peer lives as long as its text block; a change of a sensitive text keeps the name.
		owner
			.GetPropertyChangedObservable(SensitiveTextBlock.IsSensitiveProperty)
			.Merge(owner
				.GetPropertyChangedObservable(TextBlock.TextProperty)
				.Where(_ => !Block.IsSensitive))
			.Subscribe(_ => RaiseNameChanged());
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

	/// <inheritdoc />
	protected override string? GetNameCore()
	{
		return Block.IsSensitive
			? Strings.HiddenText
			: Block.Inlines?.Text ?? Block.Text;
	}

	/// <inheritdoc />
	protected override bool IsControlElementCore() => Block.TemplatedParent is null && base.IsControlElementCore();
	#endregion

	#region Helpers
	/// <summary>
	/// Tells of a new name after a change of the text or its sensitivity.
	/// </summary>
	private void RaiseNameChanged()
	{
		// The old name may be the text itself.
		RaisePropertyChangedEvent(
			AutomationElementIdentifiers.NameProperty,
			null,
			GetName());
	}
	#endregion
}
