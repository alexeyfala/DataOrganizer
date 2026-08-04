using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DialogHostAvalonia;
using System;

namespace DataOrganizer.Wrappers;

/// <summary>
/// A <see cref="UserControl" /> that takes the focus once loaded and closes the current
/// dialog on <see cref="Key.Escape" />.
/// </summary>
public abstract class DialogViewBase : UserControl
{
	#region Properties
	/// <inheritdoc />
	protected override Type StyleKeyOverride { get; } = typeof(UserControl);
	#endregion

	#region Constructors
	protected DialogViewBase() => Focusable = true;
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnKeyUp(KeyEventArgs e)
	{
		base.OnKeyUp(e);

		if (e.Key != Key.Escape)
		{
			return;
		}

		DialogHost.Close(null);
	}

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);

		Focus();
	}
	#endregion
}
