using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using AwesomeAssertions;
using DataOrganizer.Behaviors;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(MoveFocusOnEnterBehavior)}"" type")]
internal class MoveFocusOnEnterBehaviorTests
{
	#region Methods
	/// <summary>
	/// <see cref="MoveFocusOnEnterBehavior" />: keys other than Enter are not touched.
	/// </summary>
	[AvaloniaTest]
	public void Another_Key_Is_Left_Alone()
	{
		// Arrange
		(TextBox source, TextBox target) = CreateSetup(isMoveAllowed: true);

		// Act
		KeyEventArgs args = RaiseKeyDown(source, Key.A);

		// Assert
		args.Handled
			.Should()
			.BeFalse();

		target.IsFocused
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="MoveFocusOnEnterBehavior.IsMoveAllowed" />: a refused move leaves the key to the
	/// default handling.
	/// </summary>
	[AvaloniaTest]
	public void Enter_Is_Left_Alone_While_The_Move_Is_Not_Allowed()
	{
		// Arrange
		(TextBox source, TextBox target) = CreateSetup(isMoveAllowed: false);

		// Act
		KeyEventArgs args = RaiseKeyDown(source, Key.Enter);

		// Assert
		args.Handled
			.Should()
			.BeFalse();

		target.IsFocused
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="MoveFocusOnEnterBehavior.Target" />: a hidden target is left alone, so the dialog
	/// keeps its usual Enter handling.
	/// </summary>
	[AvaloniaTest]
	public void Enter_Is_Left_Alone_While_The_Target_Is_Hidden()
	{
		// Arrange
		(TextBox source, TextBox target) = CreateSetup(isMoveAllowed: true);

		target.IsVisible = false;

		// Act
		KeyEventArgs args = RaiseKeyDown(source, Key.Enter);

		// Assert
		args.Handled
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="MoveFocusOnEnterBehavior" />: Enter puts the focus on the target and keeps the key
	/// away from the default button.
	/// </summary>
	[AvaloniaTest]
	public void Enter_Moves_The_Focus_To_The_Target()
	{
		// Arrange
		(TextBox source, TextBox target) = CreateSetup(isMoveAllowed: true);

		// Act
		KeyEventArgs args = RaiseKeyDown(source, Key.Enter);

		// Assert
		args.Handled
			.Should()
			.BeTrue();

		target.IsFocused
			.Should()
			.BeTrue();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds two inputs in a shown window, the first one carrying the behavior.
	/// </summary>
	private static (TextBox Source, TextBox Target) CreateSetup(bool isMoveAllowed)
	{
		TextBox source = new();

		TextBox target = new();

		StackPanel panel = new()
		{
			Children = { source, target }
		};

		Window window = new() { Content = panel };

		window.Show();

		MoveFocusOnEnterBehavior behavior = new()
		{
			IsMoveAllowed = isMoveAllowed,
			Target = target
		};

		behavior.Attach(source);

		return (source, target);
	}

	/// <summary>
	/// Raises a key press on the input and returns the arguments it has been handled with.
	/// </summary>
	private static KeyEventArgs RaiseKeyDown(TextBox source, Key key)
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
