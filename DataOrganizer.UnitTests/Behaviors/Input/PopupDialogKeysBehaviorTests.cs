using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Input;
using System.Reflection;

namespace DataOrganizer.UnitTests.Behaviors.Input;

[TestFixture(Description = $@"Tests of ""{nameof(PopupDialogKeysBehavior)}"" type")]
internal class PopupDialogKeysBehaviorTests
{
	#region Methods
	/// <summary>
	/// <see cref="PopupDialogKeysBehavior" />: Enter in a native popup clicks its default button.
	/// </summary>
	[AvaloniaTest]
	public void Enter_Clicks_The_Default_Button_Of_A_Native_Popup()
	{
		// Arrange
		int clicks = 0;

		TextBox input = new();

		Button button = new()
		{
			IsDefault = true
		};

		button.Click += (_, _) => clicks++;

		StackPanel content = new()
		{
			Children =
			{
				input,
				button
			}
		};

		Interaction
			.GetBehaviors(content)
			.Add(new PopupDialogKeysBehavior());

		Window window = new();

		window.Show();

		PopupRoot popupRoot = ShowInNativePopup(window, content);

		input.Focus();

		// Act
		window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

		window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);

		Dispatcher.UIThread.RunJobs();

		// Closed before the assertion, otherwise the focus stays in the popup for the following tests.
		popupRoot.Dispose();

		// Assert
		clicks
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="PopupDialogKeysBehavior" />: a new line that the focused text box takes for itself does not click the
	/// default button.
	/// </summary>
	[AvaloniaTest]
	public void Enter_Taken_By_The_Focused_Control_Leaves_The_Default_Button_Alone()
	{
		// Arrange
		int clicks = 0;

		TextBox input = new()
		{
			AcceptsReturn = true
		};

		Button button = new()
		{
			IsDefault = true
		};

		button.Click += (_, _) => clicks++;

		StackPanel content = new()
		{
			Children =
			{
				input,
				button
			}
		};

		Interaction
			.GetBehaviors(content)
			.Add(new PopupDialogKeysBehavior());

		Window window = new();

		window.Show();

		PopupRoot popupRoot = ShowInNativePopup(window, content);

		input.Focus();

		// Act
		window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

		window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);

		Dispatcher.UIThread.RunJobs();

		// Closed before the assertion, otherwise the focus stays in the popup for the following tests.
		popupRoot.Dispose();

		// Assert
		clicks
			.Should()
			.Be(0);
	}

	/// <summary>
	/// <see cref="PopupDialogKeysBehavior" />: Escape in a native popup clicks its cancel button.
	/// </summary>
	[AvaloniaTest]
	public void Escape_Clicks_The_Cancel_Button_Of_A_Native_Popup()
	{
		// Arrange
		int clicks = 0;

		TextBox input = new();

		Button button = new()
		{
			IsCancel = true
		};

		button.Click += (_, _) => clicks++;

		StackPanel content = new()
		{
			Children =
			{
				input,
				button
			}
		};

		Interaction
			.GetBehaviors(content)
			.Add(new PopupDialogKeysBehavior());

		Window window = new();

		window.Show();

		PopupRoot popupRoot = ShowInNativePopup(window, content);

		input.Focus();

		// Act
		window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		window.KeyReleaseQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		Dispatcher.UIThread.RunJobs();

		// Closed before the assertion, otherwise the focus stays in the popup for the following tests.
		popupRoot.Dispose();

		// Assert
		clicks
			.Should()
			.Be(1);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Shows the content in a native popup of the window and lets the layout settle.
	/// </summary>
	private static PopupRoot ShowInNativePopup(Window window, Control content)
	{
		// The headless platform hands out overlay popups; its native popup has a private constructor.
		object windowImpl = window.PlatformImpl!;

		IPopupImpl popupImpl = (IPopupImpl)windowImpl
			.GetType()
			.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [windowImpl.GetType()])!
			.Invoke([windowImpl]);

		PopupRoot popupRoot = new(window, popupImpl, null)
		{
			Content = content
		};

		// A popup routes its events on to its logical parent, here the window.
		((ISetLogicalParent)popupRoot).SetParent(window);

		popupRoot.Show();

		Dispatcher.UIThread.RunJobs();

		return popupRoot;
	}
	#endregion
}
