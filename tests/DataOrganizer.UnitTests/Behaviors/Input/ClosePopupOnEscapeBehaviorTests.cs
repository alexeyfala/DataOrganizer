using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Input;

namespace DataOrganizer.UnitTests.Behaviors.Input;

[TestFixture(Description = $@"Tests of ""{nameof(ClosePopupOnEscapeBehavior)}"" type")]
internal class ClosePopupOnEscapeBehaviorTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClosePopupOnEscapeBehavior" />: keys other than Escape are not touched.
	/// </summary>
	[AvaloniaTest]
	public void Another_Key_Is_Left_Alone()
	{
		// Arrange
		(TextBox focused, Popup popup, _) = CreateSetup();

		popup.Open();

		// Act
		KeyEventArgs args = RaiseKeyDown(focused, Key.A);

		// Assert
		args.Handled
			.Should()
			.BeFalse();

		popup.IsOpen
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClosePopupOnEscapeBehavior" />: Escape pressed anywhere in the window closes the open popup and goes no further.
	/// </summary>
	[AvaloniaTest]
	public void Escape_Closes_The_Open_Popup()
	{
		// Arrange
		(TextBox focused, Popup popup, _) = CreateSetup();

		popup.Open();

		// Act
		KeyEventArgs args = RaiseKeyDown(focused, Key.Escape);

		// Assert
		args.Handled
			.Should()
			.BeTrue();

		popup.IsOpen
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClosePopupOnEscapeBehavior" />: Escape pressed inside the popup reaches the window and closes the popup.
	/// </summary>
	[AvaloniaTest]
	public void Escape_From_Inside_The_Popup_Closes_It()
	{
		// Arrange
		(_, Popup popup, _) = CreateSetup();

		popup.Open();

		// Act
		KeyEventArgs args = RaiseKeyDown(popup.Child!, Key.Escape);

		// Assert
		args.Handled
			.Should()
			.BeTrue();

		popup.IsOpen
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClosePopupOnEscapeBehavior" />: a detached behavior stops listening to the window.
	/// </summary>
	[AvaloniaTest]
	public void Escape_Is_Left_Alone_After_Detaching()
	{
		// Arrange
		(TextBox focused, Popup popup, ClosePopupOnEscapeBehavior behavior) = CreateSetup();

		popup.Open();

		behavior.Detach();

		// Act
		KeyEventArgs args = RaiseKeyDown(focused, Key.Escape);

		// Assert
		args.Handled
			.Should()
			.BeFalse();

		popup.IsOpen
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClosePopupOnEscapeBehavior" />: a closed popup leaves Escape to the window.
	/// </summary>
	[AvaloniaTest]
	public void Escape_Is_Left_Alone_After_The_Popup_Closes()
	{
		// Arrange
		(TextBox focused, Popup popup, _) = CreateSetup();

		popup.Open();

		popup.Close();

		// Act
		KeyEventArgs args = RaiseKeyDown(focused, Key.Escape);

		// Assert
		args.Handled
			.Should()
			.BeFalse();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds an input and a closed popup in a shown window, the popup carrying the behavior.
	/// </summary>
	private static (TextBox Focused, Popup Popup, ClosePopupOnEscapeBehavior Behavior) CreateSetup()
	{
		TextBox focused = new();

		Popup popup = new() { Child = new TextBlock() };

		StackPanel panel = new()
		{
			Children = { focused, popup }
		};

		Window window = new() { Content = panel };

		window.Show();

		ClosePopupOnEscapeBehavior behavior = new();

		behavior.Attach(popup);

		return (focused, popup, behavior);
	}

	/// <summary>
	/// Raises a key press on the element and returns the arguments it has been handled with.
	/// </summary>
	private static KeyEventArgs RaiseKeyDown(Interactive source, Key key)
	{
		KeyEventArgs args = new()
		{
			Key = key,
			RoutedEvent = InputElement.KeyDownEvent,
			Source = source
		};

		source.RaiseEvent(args);

		return args;
	}
	#endregion
}
