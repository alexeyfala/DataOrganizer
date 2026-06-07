using AwesomeAssertions;
using DataOrganizer.Helpers;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(SecureStringHelper)}"" type")]
internal class SecureStringHelperTests
{
	#region Methods
	/// <summary>
	/// <see cref="SecureStringHelper.CaptureAndWipe" />: the returned pinned secret has the same length and content as the source.
	/// </summary>
	[Test]
	public void CaptureAndWipe_Copies_Content_Into_Pinned_Secret_With_Same_Length()
	{
		// Arrange
		const string sample = "secret-payload";

		// Use a non-interned, mutable string so wiping doesn't affect literals shared elsewhere.
		string source = new(sample.ToCharArray());

		// Act
		using PinnedSecret secret = SecureStringHelper.CaptureAndWipe(source);

		// Assert
		secret.Length
			.Should()
			.Be(sample.Length);

		secret.AsReadOnlySpan().ToArray()
			.Should()
			.BeEquivalentTo(sample.ToCharArray());
	}

	/// <summary>
	/// <see cref="SecureStringHelper.CaptureAndWipe" />: the original source string is zeroed out after capture.
	/// </summary>
	[Test]
	public void CaptureAndWipe_Wipes_Original_String_Memory()
	{
		// Arrange
		string source = new("payload".ToCharArray());

		// Act
		using PinnedSecret _ = SecureStringHelper.CaptureAndWipe(source);

		// Assert
		source
			.All(c => c == '\0')
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SecureStringHelper.WipeString" />: an empty string is left untouched and no exception is thrown.
	/// </summary>
	[Test]
	public void WipeString_Does_Nothing_For_Empty_String()
	{
		// Arrange
		string source = new(Array.Empty<char>());

		// Act
		Action act = () => SecureStringHelper.WipeString(source);

		// Assert
		act
			.Should()
			.NotThrow();

		source
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="SecureStringHelper.WipeString" />: every character of the string is replaced with the null character.
	/// </summary>
	[Test]
	public void WipeString_Replaces_All_Characters_With_Null()
	{
		// Arrange
		string source = new("secret-data".ToCharArray());

		// Act
		SecureStringHelper.WipeString(source);

		// Assert
		source
			.All(c => c == '\0')
			.Should()
			.BeTrue();
	}
	#endregion
}
