using AwesomeAssertions;
using DataOrganizer.Extensions;
using SharpHook.Data;

namespace DataOrganizer.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(KeyCodeExtensions)}"" type")]
internal class KeyCodeExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="KeyCodeExtensions.IsModifierOrLock" />: not set for the keys that a hotkey is typed with.
	/// </summary>
	[Test]
	public void IsModifierOrLock_Is_Not_Set_For_Other_Keys(
		[Values(KeyCode.VcA, KeyCode.VcQ, KeyCode.Vc1, KeyCode.VcEnter, KeyCode.VcSpace)] KeyCode code)
	{
		// Act, Assert
		code.IsModifierOrLock()
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="KeyCodeExtensions.IsModifierOrLock" />: set for every modifier key and every lock key.
	/// </summary>
	[Test]
	public void IsModifierOrLock_Is_Set_For_Modifier_And_Lock_Keys(
		[Values(
			KeyCode.VcCapsLock,
			KeyCode.VcScrollLock,
			KeyCode.VcNumLock,
			KeyCode.VcLeftShift,
			KeyCode.VcRightShift,
			KeyCode.VcLeftControl,
			KeyCode.VcRightControl,
			KeyCode.VcLeftAlt,
			KeyCode.VcRightAlt,
			KeyCode.VcLeftMeta,
			KeyCode.VcRightMeta)] KeyCode code)
	{
		// Act, Assert
		code.IsModifierOrLock()
			.Should()
			.BeTrue();
	}
	#endregion
}
