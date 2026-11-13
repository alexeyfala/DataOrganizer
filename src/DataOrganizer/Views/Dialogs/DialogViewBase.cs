using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DialogHostAvalonia;
using System;

namespace DataOrganizer.Views.Dialogs;

/// <summary>
/// A <see cref="UserControl" /> that takes the focus once loaded and closes its own dialog on
/// <see cref="Key.Escape" />.
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

		// A dialog sits inside the host that shows it, and each window has a host of its own.
		if (e.Key != Key.Escape || this.FindAncestorOfType<DialogHost>() is not { CurrentSession: { } session })
		{
			return;
		}

		session.Close();
	}

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		base.OnLoaded(e);

		Focus();
	}
	#endregion
}
