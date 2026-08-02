using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using System;

namespace DataOrganizer.Wrappers;

/// <summary>
/// A <see cref="TabControl" /> that also selects a tab on a right click, so that a context menu
/// always acts on the tab it was invoked from.
/// </summary>
internal sealed class RightClickSelectableTabControl : TabControl
{
	#region Properties
	/// <inheritdoc />
	protected override Type StyleKeyOverride { get; } = typeof(TabControl);
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override bool ShouldTriggerSelection(Visual selectable, PointerEventArgs eventArgs)
	{
		return base.ShouldTriggerSelection(selectable, eventArgs)
		|| (eventArgs.Properties.PointerUpdateKind is PointerUpdateKind.RightButtonPressed
			&& ItemSelectionEventTriggers.ShouldTriggerSelection(selectable, eventArgs));
	}
	#endregion
}
