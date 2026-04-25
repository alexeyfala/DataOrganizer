using Avalonia.Media;
using AwesomeAssertions;
using DataOrganizer.Extensions;
using Serilog.Events;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(LogEventLevelExtensions)}"" type")]
internal class LogEventLevelExtensionsTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="LogEventLevelExtensions.ToBrush" />.
	/// </summary>
	[Test]
	public void ToBrush_Returns_Expected_Brush_For_Level([Values] LogEventLevel level)
	{
		// Act
		IImmutableSolidColorBrush result = level.ToBrush();

		// Assert
		IImmutableSolidColorBrush expected = level switch
		{
			LogEventLevel.Debug => Brushes.CadetBlue,
			LogEventLevel.Information => Brushes.LimeGreen,
			LogEventLevel.Warning => Brushes.Orange,
			LogEventLevel.Error or LogEventLevel.Fatal => Brushes.Red,
			_ => Brushes.Transparent
		};

		result
			.Should()
			.BeSameAs(expected);
	}

	/// <summary>
	/// Test of <see cref="LogEventLevelExtensions.ToShort" />.
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
