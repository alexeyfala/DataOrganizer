using DataOrganizer.Helpers.Execution;

namespace DataOrganizer.UnitTests.Helpers.Execution;

[TestFixture(Description = $@"Tests of ""{nameof(ExecutableFileDetector)}"" type")]
internal class ExecutableFileDetectorTests
{
	#region Methods
	/// <summary>
	/// <see cref="ExecutableFileDetector.IsExecutable" />: verifies runnable extensions are detected on every OS.
	/// </summary>
	[TestCase("app.exe", ExpectedResult = true)]
	[TestCase("application.jnlp", ExpectedResult = true)]
	[TestCase("archive.zip", ExpectedResult = false)]
	[TestCase("bundle.app", ExpectedResult = true)]
	[TestCase("C:\\folder\\app.exe", ExpectedResult = true)]
	[TestCase("document.txt", ExpectedResult = false)]
	[TestCase("entry.desktop", ExpectedResult = true)]
	[TestCase("image.png", ExpectedResult = false)]
	[TestCase("installer.msi", ExpectedResult = true)]
	[TestCase("installer.pkg", ExpectedResult = true)]
	[TestCase("invoice.pdf.exe", ExpectedResult = true)]
	[TestCase("noext.", ExpectedResult = false)]
	[TestCase("readme", ExpectedResult = false)]
	[TestCase("script.cmd", ExpectedResult = true)]
	[TestCase("script.ps1", ExpectedResult = true)]
	[TestCase("script.sh", ExpectedResult = true)]
	[TestCase("script.SH", ExpectedResult = true)]
	[TestCase("settings.settingcontent-ms", ExpectedResult = true)]
	[TestCase("shortcut.lnk", ExpectedResult = true)]
	[TestCase("tool.py", ExpectedResult = true)]
	public bool IsExecutable_Detects_Executable_Extension(string fileName)
	{
		// Act
		return ExecutableFileDetector.IsExecutable(fileName);
	}

	/// <summary>
	/// <see cref="ExecutableFileDetector.IsExecutable" />: verifies an extension hidden behind a trailing dot or space is detected.
	/// </summary>
	[TestCase("payload.cmd.", ExpectedResult = true)]
	[TestCase("payload.cmd..", ExpectedResult = true)]
	[TestCase("payload.cmd ", ExpectedResult = true)]
	[TestCase("payload.cmd . ", ExpectedResult = true)]
	[TestCase("payload.exe\u00A0", ExpectedResult = true)]
	[TestCase("report.docx.", ExpectedResult = false)]
	public bool IsExecutable_Detects_Extension_Hidden_By_Trailing_Characters(string fileName)
	{
		// Act
		return ExecutableFileDetector.IsExecutable(fileName);
	}

	/// <summary>
	/// <see cref="ExecutableFileDetector.IsExecutable" />: verifies an extension reversed for display by a Unicode override is detected.
	/// </summary>
	[TestCase("Report\u202Ecod.exe", ExpectedResult = true)]
	[TestCase("Report\u202Eexe.doc", ExpectedResult = false)]
	public bool IsExecutable_Detects_Extension_Masked_By_Unicode(string fileName)
	{
		// Act
		return ExecutableFileDetector.IsExecutable(fileName);
	}

	/// <summary>
	/// <see cref="ExecutableFileDetector.IsExecutable" />: verifies an absolute path is accepted instead of a bare name.
	/// </summary>
	[TestCase(@"C:\temp\7b1\payload.cmd.", ExpectedResult = true)]
	[TestCase(@"C:\temp\7b1\report.txt", ExpectedResult = false)]
	[TestCase("/home/user/.config/script.sh", ExpectedResult = true)]
	public bool IsExecutable_Detects_Extension_Of_Absolute_Path(string filePath)
	{
		// Act
		return ExecutableFileDetector.IsExecutable(filePath);
	}

	/// <summary>
	/// <see cref="ExecutableFileDetector.IsExecutable" />: verifies the extension is matched ignoring case.
	/// </summary>
	[TestCase("PAYLOAD.CMD", ExpectedResult = true)]
	[TestCase("Payload.Exe", ExpectedResult = true)]
	[TestCase("REPORT.DOCX", ExpectedResult = false)]
	public bool IsExecutable_Ignores_Extension_Case(string fileName)
	{
		// Act
		return ExecutableFileDetector.IsExecutable(fileName);
	}

	/// <summary>
	/// <see cref="ExecutableFileDetector.IsExecutable" />: verifies a file left without an extension is not treated as executable.
	/// </summary>
	[TestCase(null, ExpectedResult = false)]
	[TestCase("", ExpectedResult = false)]
	[TestCase("README", ExpectedResult = false)]
	[TestCase("payload.", ExpectedResult = false)]
	[TestCase("payload   ", ExpectedResult = false)]
	[TestCase(". ", ExpectedResult = false)]
	public bool IsExecutable_Skips_File_Without_Extension(string? fileName)
	{
		// Act
		return ExecutableFileDetector.IsExecutable(fileName);
	}

	/// <summary>
	/// <see cref="ExecutableFileDetector.IsExecutable" />: verifies regular documents are opened without a question.
	/// </summary>
	[TestCase("report.txt", ExpectedResult = false)]
	[TestCase("report.docx", ExpectedResult = false)]
	[TestCase("book.pdf", ExpectedResult = false)]
	[TestCase("photo.png", ExpectedResult = false)]
	[TestCase("archive.zip", ExpectedResult = false)]
	public bool IsExecutable_Skips_Regular_Document(string fileName)
	{
		// Act
		return ExecutableFileDetector.IsExecutable(fileName);
	}
	#endregion
}
