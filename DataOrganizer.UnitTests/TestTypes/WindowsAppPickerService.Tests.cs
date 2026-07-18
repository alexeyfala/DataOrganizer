using DataOrganizer.Services;
using System.Runtime.Versioning;

namespace DataOrganizer.UnitTests.TestTypes;

[SupportedOSPlatform("windows")]
[TestFixture(Description = $@"Tests of ""{nameof(WindowsAppPickerService)}"" type")]
internal class WindowsAppPickerServiceTests
{
	#region Methods
	/// <summary>
	/// <see cref="WindowsAppPickerService.ExtractExecutablePath" />: parses both quoted and unquoted "open command" forms.
	/// </summary>
	[TestCase("\"C:\\Program Files\\App\\app.exe\" \"%1\"", ExpectedResult = "C:\\Program Files\\App\\app.exe")]
	[TestCase("app.exe %1", ExpectedResult = "app.exe")]
	[TestCase("notepad.exe", ExpectedResult = "notepad.exe")]
	[TestCase("  app.exe %1  ", ExpectedResult = "app.exe")]
	[TestCase("\"C:\\App\\app.exe", ExpectedResult = null)]
	public string? ExtractExecutablePath_Parses_Open_Command(string command)
	{
		return WindowsAppPickerService.ExtractExecutablePath(command);
	}
	#endregion
}
