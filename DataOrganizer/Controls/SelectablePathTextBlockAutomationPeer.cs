using Avalonia.Automation.Peers;

namespace DataOrganizer.Controls;

/// <summary>
/// Automation peer of a <see cref="SelectablePathTextBlock" />: a text named by its path, without the marks the text
/// wraps at.
/// </summary>
internal sealed class SelectablePathTextBlockAutomationPeer : TextBlockAutomationPeer
{
	#region Constructors
	public SelectablePathTextBlockAutomationPeer(SelectablePathTextBlock owner) : base(owner)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override string? GetNameCore() => ((SelectablePathTextBlock)Owner).Path;
	#endregion
}
