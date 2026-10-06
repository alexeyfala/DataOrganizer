using Avalonia;
using Avalonia.Controls;

namespace DataOrganizer.Controls;

/// <summary>
/// A stack panel that arranges its children again after each measure, so that a child that grew while the panel
/// kept its size does not stay in its old width.
/// </summary>
internal sealed class RearrangingStackPanel : StackPanel
{
	#region Methods
	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		// Unlike WPF, Avalonia keeps the arrange of a panel measured again under a new constraint, and a panel whose
		// size stays the same gets the same rect, so its children would not be arranged again.
		InvalidateArrange();

		return base.MeasureOverride(availableSize);
	}
	#endregion
}
