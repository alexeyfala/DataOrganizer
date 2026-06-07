using AwesomeAssertions;
using DataOrganizer.Extensions;
using SharpHook.Data;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(Extensions.EventMaskExtensions)}"" type")]
internal class EventMaskExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="EventMaskExtensions.RemoveFlag" />: the specified flag is cleared while other flags remain.
	/// </summary>
	[Test]
	public void RemoveFlag_Removes_Flag_From_Source()
	{
		// Arrange
		const EventMask source = EventMask.LeftCtrl | EventMask.NumLock;

		// Act
		EventMask result = source.RemoveFlag(EventMask.NumLock);

		// Assert
		result
			.Should()
			.Be(EventMask.LeftCtrl);
	}

	/// <summary>
	/// <see cref="EventMaskExtensions.RemoveFlag" />: removing the only set flag yields <see cref="EventMask.None" />.
	/// </summary>
	[Test]
	public void RemoveFlag_Returns_None_When_All_Flags_Removed()
	{
		// Arrange
		const EventMask source = EventMask.LeftCtrl;

		// Act
		EventMask result = source.RemoveFlag(EventMask.LeftCtrl);

		// Assert
		result
			.Should()
			.Be(EventMask.None);
	}

	/// <summary>
	/// <see cref="EventMaskExtensions.RemoveFlag" />: the source is returned unchanged when the flag is absent.
	/// </summary>
	[Test]
	public void RemoveFlag_Returns_Source_Unchanged_When_Flag_Is_Not_Present()
	{
		// Arrange
		const EventMask source = EventMask.LeftCtrl;

		// Act
		EventMask result = source.RemoveFlag(EventMask.NumLock);

		// Assert
		result
			.Should()
			.Be(EventMask.LeftCtrl);
	}
	#endregion
}
