using Avalonia.Media;
using AwesomeAssertions;
using DataOrganizer.Extensions.Diagnostics;
using Serilog.Events;

namespace DataOrganizer.UnitTests.Extensions.Diagnostics;

[TestFixture(Description = $@"Tests of ""{nameof(LogEventLevelBrushExtensions)}"" type")]
internal class LogEventLevelBrushExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="LogEventLevelBrushExtensions.ToBrush" />: each level maps to its expected brush.
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
	#endregion
}
