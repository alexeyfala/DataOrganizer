using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using AwesomeAssertions;
using DataOrganizer.Behaviors;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(WipeReplacedTextBehavior)}"" type")]
internal class WipeReplacedTextBehaviorTests
{
	#region Methods
	/// <summary>
	/// <see cref="WipeReplacedTextBehavior" />: every value the input replaces is zeroed in place,
	/// so the typed prefixes leave no readable trace.
	/// </summary>
	[AvaloniaTest]
	public void Every_Replaced_Value_Is_Wiped()
	{
		// Arrange
		// new string(...) builds non-interned instances: wiping a literal would corrupt the intern pool.
		string first = new(['s']);

		string second = new(['s', 'e']);

		(_, TextBox input) = CreateSetup();

		// Act
		input.Text = first;

		input.Text = second;

		input.Text = null;

		// Assert
		first
			.Should()
			.Be(new string('\0', first.Length));

		second
			.Should()
			.Be(new string('\0', second.Length));
	}

	/// <summary>
	/// <see cref="WipeReplacedTextBehavior" />: the current value stays readable, only the replaced one is wiped.
	/// </summary>
	[AvaloniaTest]
	public void The_Current_Value_Is_Left_Alone()
	{
		// Arrange
		string typed = new(['s', 'e', 'c', 'r', 'e', 't']);

		(_, TextBox input) = CreateSetup();

		// Act
		input.Text = typed;

		// Assert
		input.Text
			.Should()
			.Be("secret");
	}

	/// <summary>
	/// <see cref="WipeReplacedTextBehavior" />: a detached behavior leaves later values alone.
	/// </summary>
	[AvaloniaTest]
	public void The_Detached_Behavior_Wipes_Nothing()
	{
		// Arrange
		string typed = new(['s', 'e', 'c', 'r', 'e', 't']);

		(WipeReplacedTextBehavior sut, TextBox input) = CreateSetup();

		input.Text = typed;

		// Act
		sut.Detach();

		input.Text = null;

		// Assert
		typed
			.Should()
			.Be("secret");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds the behavior attached to an input.
	/// </summary>
	private static (WipeReplacedTextBehavior Sut, TextBox Input) CreateSetup()
	{
		TextBox input = new();

		WipeReplacedTextBehavior sut = new();

		sut.Attach(input);

		return (sut, input);
	}
	#endregion
}
