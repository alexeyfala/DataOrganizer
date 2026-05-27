using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using System;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Closes the attached <see cref="Window" /> as soon as it loses activation
/// (focus moved to any other window).
/// </summary>
internal sealed class CloseWindowOnDeactivatedBehavior : Behavior<Window>
{
	#region Event Handlers
	/// <summary>
	/// <see cref="Window.Deactivated" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_Deactivated(object? sender, EventArgs e) => AssociatedObject?.Close();
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttached()
	{
		base.OnAttached();

		AssociatedObject?.Deactivated += AssociatedObject_Deactivated;
	}

	/// <inheritdoc />
	protected override void OnDetaching()
	{
		base.OnDetaching();

		AssociatedObject?.Deactivated -= AssociatedObject_Deactivated;
	}
	#endregion
}
