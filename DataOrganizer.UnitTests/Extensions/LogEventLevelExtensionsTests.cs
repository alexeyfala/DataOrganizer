using Avalonia.Media;
using AwesomeAssertions;
using DataOrganizer.Extensions;
using Serilog.Events;

namespace DataOrganizer.UnitTests.Extensions;

[TestFixture(Description = $@"Tests of ""{nameof(LogEventLevelExtensions)}"" type")]
internal class LogEventLevelExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="LogEventLevelExtensions.ToBrush" />: each level maps to its expected brush.
	/// </summary>
	[TestCase(LogEventLevel.Verbose, nameof(Colors.Gray))]
	[TestCase(LogEventLevel.Debug, nameof(Colors.CadetBlue))]
	[TestCase(LogEventLevel.Information, nameof(Colors.LimeGreen))]
	[TestCase(LogEventLevel.Warning, nameof(Colors.Orange))]
	[TestCase(LogEventLevel.Error, nameof(Colors.Red))]
	[TestCase(LogEventLevel.Fatal, nameof(Colors.Red))]
	public void ToBrush_Returns_Expected_Brush_For_Level(LogEventLevel level, string expected)
	{
		// Act
		IImmutableSolidColorBrush result = level.ToBrush();

		// Assert
		result
			.Color
			.Should()
			.Be(Color.Parse(expected));
	}

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
