using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using System;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Closes the associated <see cref="Window" /> when it loses focus, unless <see cref="KeepOpen" /> is set.
/// </summary>
internal sealed class CloseWindowOnDeactivatedBehavior : Behavior<Window>
{
	#region Properties
	/// <summary>
	/// When <see langword="true" />, focus loss leaves the window open.
	/// </summary>
	public bool KeepOpen
	{
		get => GetValue(KeepOpenProperty);
		set => SetValue(KeepOpenProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="KeepOpen" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> KeepOpenProperty = AvaloniaProperty
		.Register<CloseWindowOnDeactivatedBehavior, bool>(name: nameof(KeepOpen));
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="Window.Deactivated" /> handler of <see cref="AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_Deactivated(object? sender, EventArgs e)
	{
		if (KeepOpen)
		{
			return;
		}

		AssociatedObject?.Close();
	}
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
