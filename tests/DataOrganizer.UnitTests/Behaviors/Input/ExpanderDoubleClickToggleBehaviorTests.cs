using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Input;
using DataOrganizer.UnitTests.Factories;

namespace DataOrganizer.UnitTests.Behaviors.Input;

[TestFixture(Description = $@"Tests of ""{nameof(ExpanderDoubleClickToggleBehavior)}"" type")]
internal class ExpanderDoubleClickToggleBehaviorTests
{
	#region Methods
	/// <summary>
	/// <see cref="ExpanderDoubleClickToggleBehavior" />: a button inside the header takes a single click, and the expander
	/// stays as it is.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_A_Button_Inside_The_Header_Reaches_The_Button()
	{
		// Arrange
		int clickCount = 0;

		Button button = new()
		{
			Content = "Copy"
		};

		button.Click += (_, _) => clickCount++;

		// A background lets the header take the presses around the button.
		Grid header = new()
		{
			Background = Brushes.Transparent,
			Children =
			{
				button
			}
		};

		Interaction
			.GetBehaviors(header)
			.Add(new ExpanderDoubleClickToggleBehavior());

		Expander expander = new()
		{
			Content = "Records",
			Header = header
		};

		Window window = HeadlessWindow.Show(expander);

		// Act
		window.Click(button);

		// Assert
		clickCount
			.Should()
			.Be(1);

		expander.IsExpanded
			.Should()
			.BeFalse();
	}
	#endregion
}
