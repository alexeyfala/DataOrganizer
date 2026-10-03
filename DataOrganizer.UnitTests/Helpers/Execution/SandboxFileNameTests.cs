using AwesomeAssertions;
using DataOrganizer.Helpers.Execution;
using System.Linq;

namespace DataOrganizer.UnitTests.Helpers.Execution;

[TestFixture(Description = $@"Tests of ""{nameof(SandboxFileName)}"" type")]
internal class SandboxFileNameTests
{
	#region Methods
	/// <summary>
	/// <see cref="SandboxFileName.Create" />: cuts a long name between characters, never inside one.
	/// </summary>
	[Test]
	public void Create_Cuts_A_Long_Name_Between_Characters()
	{
		// Arrange
		const string wave = "👋";

		string name = $"{string.Concat(Enumerable.Repeat(wave, 100))}.txt";

		// Act
		string result = SandboxFileName.Create(name);

		// Assert
		result
			.Should()
			.Be($"{string.Concat(Enumerable.Repeat(wave, 62))}.txt");
	}

	/// <summary>
	/// <see cref="SandboxFileName.Create" />: cuts a long name down to the longest file name and keeps its extension.
	/// </summary>
	[Test]
	public void Create_Cuts_A_Long_Name_Keeping_The_Extension()
	{
		// Arrange
		string name = $"{new string('n', 300)}.txt";

		// Act
		string result = SandboxFileName.Create(name);

		// Assert
		result
			.Should()
			.Be($"{new string('n', 251)}.txt");
	}

	/// <summary>
	/// <see cref="SandboxFileName.Create" />: drops the trailing dots and spaces that Windows drops, and names a file left
	/// with no name.
	/// </summary>
	[TestCase("notes.", ExpectedResult = "notes")]
	[TestCase("notes.txt ", ExpectedResult = "notes.txt")]
	[TestCase("notes . .", ExpectedResult = "notes")]
	[TestCase(" ", ExpectedResult = "_")]
	[TestCase("...", ExpectedResult = "_")]
	[TestCase("", ExpectedResult = "_")]
	public string Create_Drops_The_Tail_Windows_Drops(string name)
	{
		// Act
		return SandboxFileName.Create(name);
	}

	/// <summary>
	/// <see cref="SandboxFileName.Create" />: keeps a name that every file system accepts as it is.
	/// </summary>
	[TestCase("notes.txt")]
	[TestCase("archive.tar.gz")]
	[TestCase(".gitignore")]
	[TestCase("Привет 👋.txt")]
	[TestCase("CONSOLE.txt")]
	[TestCase("COM10.log")]
	public void Create_Keeps_A_Name_Every_File_System_Accepts(string name)
	{
		// Act, Assert
		SandboxFileName
			.Create(name)
			.Should()
			.Be(name);
	}

	/// <summary>
	/// <see cref="SandboxFileName.Create" />: leaves no directory in the name, so the file stays in its own directory.
	/// </summary>
	[TestCase("../secret.txt", ExpectedResult = ".._secret.txt")]
	[TestCase(@"..\secret.txt", ExpectedResult = ".._secret.txt")]
	[TestCase("/etc/passwd", ExpectedResult = "_etc_passwd")]
	[TestCase(@"C:\Windows\notepad.exe", ExpectedResult = "C__Windows_notepad.exe")]
	[TestCase("..", ExpectedResult = "_")]
	[TestCase(".", ExpectedResult = "_")]
	public string Create_Keeps_The_File_In_Its_Directory(string name)
	{
		// Act
		return SandboxFileName.Create(name);
	}

	/// <summary>
	/// <see cref="SandboxFileName.Create" />: marks the name of a device, alone or before an extension.
	/// </summary>
	[TestCase("CON", ExpectedResult = "_CON")]
	[TestCase("con.txt", ExpectedResult = "_con.txt")]
	[TestCase("nul.tar.gz", ExpectedResult = "_nul.tar.gz")]
	[TestCase("AUX .txt", ExpectedResult = "_AUX .txt")]
	[TestCase("PRN.", ExpectedResult = "_PRN")]
	[TestCase("COM1.log", ExpectedResult = "_COM1.log")]
	[TestCase("LPT9", ExpectedResult = "_LPT9")]
	[TestCase("COM¹.txt", ExpectedResult = "_COM¹.txt")]
	public string Create_Marks_The_Name_Of_A_Device(string name)
	{
		// Act
		return SandboxFileName.Create(name);
	}

	/// <summary>
	/// <see cref="SandboxFileName.Create" />: replaces each character that Windows refuses in a file name.
	/// </summary>
	[TestCase("report?.txt", ExpectedResult = "report_.txt")]
	[TestCase("a*b.txt", ExpectedResult = "a_b.txt")]
	[TestCase("say \"hi\".txt", ExpectedResult = "say _hi_.txt")]
	[TestCase("x<y>.txt", ExpectedResult = "x_y_.txt")]
	[TestCase("a|b.txt", ExpectedResult = "a_b.txt")]
	[TestCase("a:b.txt", ExpectedResult = "a_b.txt")]
	[TestCase("tab\there.txt", ExpectedResult = "tab_here.txt")]
	public string Create_Replaces_The_Characters_Windows_Refuses(string name)
	{
		// Act
		return SandboxFileName.Create(name);
	}
	#endregion
}
