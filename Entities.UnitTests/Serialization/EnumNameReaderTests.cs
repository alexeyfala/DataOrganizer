using AwesomeAssertions;
using Entities.Serialization;
using SharpHook.Data;

namespace Entities.UnitTests.Serialization;

[TestFixture(Description = $@"Tests of ""{nameof(EnumNameReader)}"" type")]
internal class EnumNameReaderTests
{
	#region Methods
	/// <summary>
	/// <see cref="EnumNameReader.Read{T}" />: only the text a written name has is read, so a stored
	/// value that is empty, a number or a signed number falls back instead of becoming a flag.
	/// </summary>
	[TestCase("")]
	[TestCase("1")]
	[TestCase("-1")]
	[TestCase("+1")]
	public void Read_Falls_Back_For_Anything_But_A_Written_Name(string name)
	{
		// Act
		EventMask result = EnumNameReader.Read(name, EventMask.None);

		// Assert
		result
			.Should()
			.Be(EventMask.None);
	}

	/// <summary>
	/// <see cref="EnumNameReader.Read{T}" />: a written name is read back as the value it names.
	/// </summary>
	[Test]
	public void Read_Reads_A_Written_Name()
	{
		// Act
		EventMask result = EnumNameReader.Read(nameof(EventMask.LeftCtrl), EventMask.None);

		// Assert
		result
			.Should()
			.Be(EventMask.LeftCtrl);
	}
	#endregion
}
