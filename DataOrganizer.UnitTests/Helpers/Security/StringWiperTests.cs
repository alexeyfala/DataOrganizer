using AwesomeAssertions;
using DataOrganizer.Helpers.Security;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.Helpers.Security;

[TestFixture(Description = $@"Tests of ""{nameof(StringWiper)}"" type")]
internal class StringWiperTests
{
	#region Methods
	/// <summary>
	/// <see cref="StringWiper.CaptureAndWipe" />: the returned pinned secret has the same length and content as the source.
	/// </summary>
	[Test]
	public void CaptureAndWipe_Copies_Content_Into_Pinned_Secret_With_Same_Length()
	{
		// Arrange
		const string sample = "secret-payload";

		// Use a non-interned, mutable string so wiping doesn't affect literals shared elsewhere.
		string source = new(sample.ToCharArray());

		// Act
		using PinnedSecret secret = StringWiper.CaptureAndWipe(source);

		// Assert
		secret.Length
			.Should()
			.Be(sample.Length);

		secret.AsReadOnlySpan().ToArray()
			.Should()
			.BeEquivalentTo(sample.ToCharArray());
	}

	/// <summary>
	/// <see cref="StringWiper.CaptureAndWipe" />: the original source string is zeroed out after capture.
	/// </summary>
	[Test]
	public void CaptureAndWipe_Wipes_Original_String_Memory()
	{
		// Arrange
		string source = new("payload".ToCharArray());

		// Act
		using PinnedSecret _ = StringWiper.CaptureAndWipe(source);

		// Assert
		source
			.All(c => c == '\0')
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="StringWiper.Wipe" />: an empty string is left untouched and no exception is thrown.
	/// </summary>
	[Test]
	public void Wipe_Does_Nothing_For_Empty_String()
	{
		// Arrange
		string source = new([]);

		// Act
		Action act = () => StringWiper.Wipe(source);

		// Assert
		act
			.Should()
			.NotThrow();

		source
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="StringWiper.Wipe" />: an interned instance is left alone, since the
	/// intern pool is shared by the whole process.
	/// </summary>
	[Test]
	public void Wipe_Leaves_An_Interned_String_Alone()
	{
		// Arrange
		string interned = string.Intern(new(['s', 'h', 'a', 'r', 'e', 'd']));

		// Act
		StringWiper.Wipe(interned);

		// Assert
		interned
			.Should()
			.Be(new string(['s', 'h', 'a', 'r', 'e', 'd']));
	}

	/// <summary>
	/// <see cref="StringWiper.Wipe" />: every character of the string is replaced with the null character.
	/// </summary>
	[Test]
	public void Wipe_Replaces_All_Characters_With_Null()
	{
		// Arrange
		string source = new("secret-data".ToCharArray());

		// Act
		StringWiper.Wipe(source);

		// Assert
		source
			.All(c => c == '\0')
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="StringWiper.Wipe" />: the guard compares instances, so a copy of an
	/// interned string is still wiped and the pooled one survives.
	/// </summary>
	[Test]
	public void Wipe_Wipes_A_Copy_Of_An_Interned_String()
	{
		// Arrange
		string interned = string.Intern(new(['t', 'w', 'i', 'n']));

		string copy = new(['t', 'w', 'i', 'n']);

		// Act
		StringWiper.Wipe(copy);

		// Assert
		copy
			.All(c => c == '\0')
			.Should()
			.BeTrue();

		interned
			.Should()
			.Be(new string(['t', 'w', 'i', 'n']));
	}
	#endregion
}
