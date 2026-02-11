using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit;
using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.DTO;
using DataOrganizer.Extensions;

namespace DataOrganizer.Abstract;

public abstract partial class TextEditorViewModelBase : EditorViewModelBase
{
	#region Auto-Generated Properties
	/// <inheritdoc cref="FileProperties.FontSize" />
	[ObservableProperty]
	private double _fontSize = 14.0;

	/// <inheritdoc cref="FileProperties.IsWordWrap" />
	[ObservableProperty]
	private bool _isWordWrap;
	#endregion

	#region Constructors
	protected TextEditorViewModelBase(Application app) : base(app)
	{
	}
	#endregion

	#region Methods
	/// <summary>
	/// Subscribes to <see cref="InputElement.PointerWheelChangedEvent" /> of <see cref="TextEditor" />.
	/// </summary>
	protected void SubscribePointerWheelChanged(TextEditor editor)
	{
		editor.AddHandler(
			InputElement.PointerWheelChangedEvent,
			Editor_PointerWheelChanged,
			RoutingStrategies.Tunnel);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerWheelChangedEvent" /> handler of <see cref="TextEditor" />.
	/// </summary>
	private void Editor_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
	{
		if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
		{
			return;
		}

		e.Handled = true;

		SpinDirection direction;

		switch (e.Delta.Y)
		{
			case > 0: // Wheel Up
				direction = SpinDirection.Increase;
				break;

			case < 0: // Wheel Down
				direction = SpinDirection.Decrease;
				break;

			default:
				return;
		}

		direction.IncreaseDecrease(FontSize, () => FontSize);
	}
	#endregion
}
