using AwesomeAssertions;
using Diagnostics.Extensions;
using Serilog.Events;

namespace Diagnostics.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(LogEventLevelExtensions)}"" type")]
internal class LogEventLevelExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="LogEventLevelExtensions.ToShort" />: each level maps to its expected short text.
	/// </summary>
	[TestCase(LogEventLevel.Verbose, "[VERBOSE]")]
	[TestCase(LogEventLevel.Debug, "[DBG]")]
	[TestCase(LogEventLevel.Information, "[INF]")]
	[TestCase(LogEventLevel.Warning, "[WRN]")]
	[TestCase(LogEventLevel.Error, "[ERR]")]
	[TestCase(LogEventLevel.Fatal, "[FTL]")]
	public void ToShort_Returns_Expected_Short_Text_For_Level(LogEventLevel level, string expected)
	{
		// Act
		string result = level.ToShort();

		// Assert
		result
			.Should()
			.Be(expected);
	}
	#endregion
}
