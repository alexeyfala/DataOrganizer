using AwesomeAssertions;
using Repository.Dto;
using SharpHook.Data;

namespace Repository.UnitTests.Dto;

[TestFixture(Description = $@"Tests of ""{nameof(KeyStroke)}"" type")]
internal class KeyStrokeTests
{
	#region Methods
	/// <summary>
	/// <see cref="KeyStroke.GetKeyName" />: the name of the key comes without the prefix the library writes, and a value
	/// without a name stays as it is.
	/// </summary>
	[TestCase(KeyCode.VcA, "A")]
	[TestCase(KeyCode.VcF13, "F13")]
	[TestCase((KeyCode)60000, "60000")]
	public void GetKeyName_Drops_The_Prefix_Of_The_Library(KeyCode code, string expected)
	{
		// Arrange
		KeyStroke sut = new()
		{
			Code = code,
			Mask = EventMask.None
		};

		// Act
		string name = sut.GetKeyName();

		// Assert
		name
			.Should()
			.Be(expected);
	}
	#endregion
}
