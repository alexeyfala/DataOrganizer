using AwesomeAssertions;
using DataOrganizer.Extensions;
using SharpHook.Data;
using EventMaskExtensions = DataOrganizer.Extensions.EventMaskExtensions;

namespace DataOrganizer.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(EventMaskExtensions)}"" type")]
internal class EventMaskExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="EventMaskExtensions.ToModifiers" />: the modifier keys stay, while the lock states, the mouse buttons and
	/// the flags of the event go.
	/// </summary>
	[Test]
	public void ToModifiers_Keeps_Only_The_Modifier_Keys()
	{
		// Arrange
		const EventMask modifiers = EventMask.LeftCtrl | EventMask.RightShift | EventMask.LeftAlt | EventMask.RightMeta;

		const EventMask mask = modifiers
			| EventMask.CapsLock
			| EventMask.NumLock
			| EventMask.ScrollLock
			| EventMask.Button1
			| EventMask.SimulatedEvent;

		// Act
		EventMask result = mask.ToModifiers();

		// Assert
		result
			.Should()
			.Be(modifiers);
	}
	#endregion
}
