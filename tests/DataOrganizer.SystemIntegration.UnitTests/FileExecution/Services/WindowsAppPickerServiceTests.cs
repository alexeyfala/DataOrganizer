using DataOrganizer.SystemIntegration.FileExecution.Services;
using System.Runtime.Versioning;

namespace DataOrganizer.SystemIntegration.UnitTests.FileExecution.Services;

/// <summary>
/// Tests of <see cref="WindowsAppPickerService" /> type.
/// </summary>
[SupportedOSPlatform("windows")]
[TestFixture]
internal sealed class WindowsAppPickerServiceTests
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
