using AwesomeAssertions;
using Entities.Serialization;
using SharpHook.Data;

namespace Entities.UnitTests.Serialization;

[TestFixture(Description = $@"Tests of ""{nameof(EnumNameReader)}"" type")]
internal class EnumNameReaderTests
{
	#region Methods
	/// <summary>
	/// <see cref="EnumNameReader.Read{T}" />: a signed number falls back even for an enum whose type
	/// parses it and prints it back unchanged.
	/// </summary>
	[Test]
	public void Read_Falls_Back_For_A_Negative_Number_The_Enum_Can_Parse()
	{
		// Act
		SignedValue result = EnumNameReader.Read("-1", SignedValue.None);

		// Assert
		result
			.Should()
			.Be(SignedValue.None);
	}

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

	#region Nested Types
	/// <summary>
	/// Signed enum: unlike the unsigned ones the application stores, its type parses a negative number,
	/// which is the only way to reach the guard refusing a sign.
	/// </summary>
	private enum SignedValue
	{
		None = 0,
		First = 1
	}
	#endregion
}
