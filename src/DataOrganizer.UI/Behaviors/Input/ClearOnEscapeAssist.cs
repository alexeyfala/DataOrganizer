using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;

namespace DataOrganizer.UI.Behaviors.Input;

/// <summary>
/// Lets Escape clear a text box that holds text; in an empty box the key goes on to the ancestors.
/// </summary>
/// <remarks>
/// A key binding of the box itself, as Avalonia runs the key bindings of the focused element and of its ancestors
/// before the routed key events, so a handler of those would lose to an ancestor that closes on Escape.
/// </remarks>
internal static class ClearOnEscapeAssist
{
	#region Styled Properties
	/// <summary>
	/// Identifies the attached property that turns the clearing on, so a style can set it.
	/// </summary>
	public static readonly AttachedProperty<bool> IsEnabledProperty = AvaloniaProperty
		.RegisterAttached<TextBox, bool>(name: "IsEnabled", ownerType: typeof(ClearOnEscapeAssist));
	#endregion

	#region Data
	/// <summary>
	/// Gesture of the key binding, which also tells it from the other bindings of the box.
	/// </summary>
	private static readonly KeyGesture EscapeGesture = new(Key.Escape);
	#endregion

	#region Constructors
	/// <summary>
	/// Follows the changes of <see cref="IsEnabledProperty" />.
	/// </summary>
	static ClearOnEscapeAssist()
	{
		IsEnabledProperty
			.Changed
			.AddClassHandler<TextBox, bool>(IsEnabledProperty_Changed);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="IsEnabledProperty" /> changed handler: adds or removes the key binding.
	/// </summary>
	private static void IsEnabledProperty_Changed(TextBox textBox, AvaloniaPropertyChangedEventArgs<bool> e)
	{
		if (!e.NewValue.GetValueOrDefault())
		{
			textBox
				.KeyBindings
				.RemoveAll(static x => ReferenceEquals(x.Gesture, EscapeGesture));

			return;
		}

		// A binding takes the key only while its command can run, so an empty box passes Escape on.
		textBox.KeyBindings.Add(new KeyBinding
		{
			Command = new RelayCommand(textBox.Clear, () => !string.IsNullOrEmpty(textBox.Text)),
			Gesture = EscapeGesture
		});
	}
	#endregion

	#region Methods
	/// <summary>
	/// Returns whether Escape clears <paramref name="textBox" />.
	/// </summary>
	public static bool GetIsEnabled(TextBox textBox) => textBox.GetValue(IsEnabledProperty);

	/// <summary>
	/// Sets whether Escape clears <paramref name="textBox" />.
	/// </summary>
	public static void SetIsEnabled(TextBox textBox, bool value) => textBox.SetValue(IsEnabledProperty, value);
	#endregion
}
