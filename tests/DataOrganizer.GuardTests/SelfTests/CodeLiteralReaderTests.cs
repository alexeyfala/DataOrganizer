using AwesomeAssertions;
using DataOrganizer.GuardTests.TestHelpers;
using DataOrganizer.GuardTests.TestHelpers.Dto;
using System.Linq;

namespace DataOrganizer.GuardTests.SelfTests;

/// <summary>
/// Tests of <see cref="CodeLiteralReader" /> type.
/// </summary>
[TestFixture]
internal sealed class CodeLiteralReaderTests
{
	#region Methods
	/// <summary>
	/// <see cref="CodeLiteralReader.ReadCode" />: of an interpolated string only the code of its holes stays, whatever the
	/// form of the string and whatever literals its holes hold.
	/// </summary>
	[TestCase("x($\"found: {string.Join(\", \", names)}\");", "x( {string.Join( , names)} );")]
	[TestCase("$\"{s.Trim('\"')}\"", " {s.Trim( )} ")]
	[TestCase("$@\"\"\"{a}\"\" {string.Join(\", \", b)}\"", " {a} {string.Join( , b)} ")]
	[TestCase("$$\"\"\"{a} {{b}}\"\"\"", " {b} ")]
	[TestCase("$\"{x switch\n{\n\t1 => \"one\",\n\t_ => \"many\"\n}}\"", " {x switch\n{\n\t1 =>  ,\n\t_ =>  \n}} ")]
	[TestCase("$\"{(x ? $\"<{y}>\" : \"\")}\"", " {(x ?  {y}  :  )} ")]
	[TestCase("$\"{{a}} {b:N2}\"", " {b} ")]
	public void ReadCode_Keeps_Only_The_Code_Of_The_Holes(string text, string expected)
	{
		// Act
		string code = CodeLiteralReader.ReadCode(text);

		// Assert
		code
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="CodeLiteralReader.ReadCode" />: each comment and plain literal becomes a space, and a quote that closes
	/// no character literal stays code.
	/// </summary>
	[TestCase("a(\"// b\"); // c", "a( );  ")]
	[TestCase("a /* \"b\" */ c", "a   c")]
	[TestCase("@\"a\"\"b\" + \"\"\"c\"d\"\"\"", "  +  ")]
	[TestCase("'\\'' + '\"'", "  +  ")]
	[TestCase("#region It's", "#region It's")]
	public void ReadCode_Turns_Comments_And_Literals_Into_Spaces(string text, string expected)
	{
		// Act
		string code = CodeLiteralReader.ReadCode(text);

		// Assert
		code
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="CodeLiteralReader.ReadInterpolatedStrings" />: a string in a hole of another is read too.
	/// </summary>
	[Test]
	public void ReadInterpolatedStrings_Reads_The_Strings_In_A_Hole()
	{
		// Arrange
		const string text = "$\"{(x ? $\"<{y}>\" : \"\")}\"";

		// Act
		string[] values = [.. CodeLiteralReader.ReadInterpolatedStrings(text).Select(static x => x.Value)];

		// Assert
		values
			.Should()
			.BeEquivalentTo(text, "$\"<{y}>\"");
	}

	/// <summary>
	/// <see cref="CodeLiteralReader.ReadInterpolatedStrings" />: a string gives its place, its text pieces and the code of
	/// its holes.
	/// </summary>
	[Test]
	public void ReadInterpolatedStrings_Reads_The_Text_And_The_Holes()
	{
		// Arrange
		const string value = "$\"say \\\"{name}\\\" to {string.Join(\", \", others)}\"";

		// Act
		InterpolatedString[] strings = [.. CodeLiteralReader.ReadInterpolatedStrings($"x = {value};")];

		// Assert
		strings
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeEquivalentTo(new InterpolatedString
			{
				Holes = ["name", "string.Join( , others)"],
				Index = 4,
				Texts = ["say \"", "\" to ", string.Empty],
				Value = value
			});
	}

	/// <summary>
	/// <see cref="CodeLiteralReader.ReadInterpolatedStrings" />: what only looks like an interpolated string inside
	/// a comment or a plain literal is no string.
	/// </summary>
	[Test]
	public void ReadInterpolatedStrings_Skips_The_Strings_In_Comments_And_Literals()
	{
		// Arrange
		const string text = "// $\"{a}\"\n\"$\\\"{b}\\\"\" /* $\"{c}\" */";

		// Act
		InterpolatedString[] strings = [.. CodeLiteralReader.ReadInterpolatedStrings(text)];

		// Assert
		strings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="CodeLiteralReader.ReadInterpolatedStrings" />: the text around a hole has its escaped quotes and braces
	/// unescaped, and any other escape stays as written.
	/// </summary>
	[TestCase("$\"\\\"{a}\\\"\"", "\"", "\"")]
	[TestCase("$@\"\"\"{a}\"\"\"", "\"", "\"")]
	[TestCase("$\"{{{a}}}\"", "{", "}")]
	[TestCase("$$\"\"\"{{{a}}}\"\"\"", "{", "}")]
	[TestCase("$\"\\\\{a}\"", "\\\\", "")]
	public void ReadInterpolatedStrings_Unescapes_The_Text_Around_A_Hole(string text, string before, string after)
	{
		// Act
		InterpolatedString[] strings = [.. CodeLiteralReader.ReadInterpolatedStrings(text)];

		// Assert
		strings
			.Should()
			.ContainSingle()
			.Which
			.Texts
			.Should()
			.Equal(before, after);
	}
	#endregion
}
