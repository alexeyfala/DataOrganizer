using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Overrides the "Hand" cursor that Material.Avalonia's Expander template hard-codes on
/// its <c>PART_ToggleButton</c>. The template value is at <c>Template</c> priority, so a
/// regular <c>Style</c> setter can't beat it; writing the value as a local property on
/// the realized element after <c>TemplateApplied</c> wins via the priority chain.
/// </summary>
internal sealed class OverrideExpanderToggleCursorBehavior : Behavior<Expander>
{
	#region Data
	private static readonly Cursor DefaultCursor = new(StandardCursorType.Arrow);
	#endregion

	#region Event Handlers
	private static void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
	{
		if (e.NameScope.Find<ToggleButton>("PART_ToggleButton") is not { } toggle)
		{
			return;
		}

		toggle.Cursor = DefaultCursor;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		if (AssociatedObject is null)
		{
			return;
		}

		AssociatedObject.TemplateApplied += OnTemplateApplied;
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		AssociatedObject?.TemplateApplied -= OnTemplateApplied;

		base.OnDetaching();
	}
	#endregion
}
