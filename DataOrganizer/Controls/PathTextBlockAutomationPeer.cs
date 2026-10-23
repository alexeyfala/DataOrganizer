using Avalonia.Automation.Peers;

namespace DataOrganizer.Controls;

/// <summary>
/// Automation peer of a <see cref="PathTextBlock" />: a text named by its path, without the marks the text wraps at.
/// </summary>
internal sealed class PathTextBlockAutomationPeer : TextBlockAutomationPeer
{
	#region Constructors
	public PathTextBlockAutomationPeer(PathTextBlock owner)
		: base(owner)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override string? GetNameCore() => ((PathTextBlock)Owner).Path;
	#endregion
}
