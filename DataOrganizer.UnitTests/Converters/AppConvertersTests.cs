using Avalonia.Data.Converters;
using AwesomeAssertions;
using DataOrganizer.Converters;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Enums.Documents;
using System;
using System.Globalization;

namespace DataOrganizer.UnitTests.Converters;

[TestFixture(Description = $@"Tests of ""{nameof(AppConverters)}"" type")]
internal class AppConvertersTests
{
	#region Methods
	/// <summary>
	/// <see cref="AppConverters.AutoLockIsExpiring" />: the warning starts within the last twenty seconds.
	/// </summary>
	[TestCase(21.0, false)]
	[TestCase(20.0, true)]
	[TestCase(1.0, true)]
	public void AutoLockIsExpiring_Warns_About_The_Last_Seconds(double seconds, bool expected)
	{
		// Act
		object? result = Convert(AppConverters.AutoLockIsExpiring, TimeSpan.FromSeconds(seconds));

		// Assert
		result
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="AppConverters.AutoLockIsExpiring" />: a countdown that is not running never warns.
	/// </summary>
	[Test]
	public void AutoLockIsExpiring_Without_A_Countdown_Is_False()
	{
		// Act
		object? result = Convert(AppConverters.AutoLockIsExpiring, null);

		// Assert
		result
			.Should()
			.Be(false);
	}

	/// <summary>
	/// <see cref="AppConverters.AutoLockRemaining" />: an hour or more keeps its hours in the caption.
	/// </summary>
	[Test]
	public void AutoLockRemaining_Keeps_The_Hours_Of_The_Longest_Delay()
	{
		// Act
		object? result = Convert(AppConverters.AutoLockRemaining, TimeSpan.FromMinutes(60.0));

		// Assert
		result
			.Should()
			.BeOfType<string>()
			.Which
			.Should()
			.Contain("1:00:00");
	}

	/// <summary>
	/// <see cref="AppConverters.AutoLockRemaining" />: the time left is shown as minutes and seconds.
	/// </summary>
	[Test]
	public void AutoLockRemaining_Shows_Minutes_And_Seconds()
	{
		// Act
		object? result = Convert(AppConverters.AutoLockRemaining, TimeSpan.FromSeconds(577.0));

		// Assert
		result
			.Should()
			.BeOfType<string>()
			.Which
			.Should()
			.Contain("09:37");
	}

	/// <summary>
	/// <see cref="AppConverters.AutoLockRemaining" />: a countdown that is not running has no caption.
	/// </summary>
	[Test]
	public void AutoLockRemaining_Without_A_Countdown_Is_Empty()
	{
		// Act
		object? result = Convert(AppConverters.AutoLockRemaining, null);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="AppConverters.LineEndingToCaption" />: line endings of one style are named after their system and characters.
	/// </summary>
	[TestCase(LineEnding.CrLf, "Windows (CR LF)")]
	[TestCase(LineEnding.Lf, "Unix (LF)")]
	[TestCase(LineEnding.Cr, "Macintosh (CR)")]
	public void LineEndingToCaption_Names_The_Line_Break(LineEnding ending, string expected)
	{
		// Act
		object? result = Convert(AppConverters.LineEndingToCaption, ending);

		// Assert
		result
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="AppConverters.LineEndingToCaption" />: a document without line breaks has no caption.
	/// </summary>
	[Test]
	public void LineEndingToCaption_Without_Line_Breaks_Is_Empty()
	{
		// Act
		object? result = Convert(AppConverters.LineEndingToCaption, LineEnding.None);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="AppConverters.NumberCaption" />: puts the number, with its digits grouped, into the format given as the parameter.
	/// </summary>
	[Test]
	public void NumberCaption_Groups_The_Digits()
	{
		// Act
		object? result = Convert(AppConverters.NumberCaption, 1234567, "[{0}]");

		// Assert
		result
			.Should()
			.Be($"[{1234567.ToString("N0", CultureInfo.CurrentCulture)}]");
	}

	/// <summary>
	/// <see cref="AppConverters.SelectionOrPosition" />: while nothing is selected, shows the caret position counted from one.
	/// </summary>
	[Test]
	public void SelectionOrPosition_Shows_The_Position_Counted_From_One()
	{
		// Arrange
		DocumentStatus status = default(DocumentStatus) with
		{
			CaretOffset = 776
		};

		// Act
		object? result = Convert(AppConverters.SelectionOrPosition, status);

		// Assert
		result
			.Should()
			.BeOfType<string>()
			.Which
			.Should()
			.Contain("777");
	}

	/// <summary>
	/// <see cref="AppConverters.SelectionOrPosition" />: a selection shows its characters and lines instead of the caret position.
	/// </summary>
	[Test]
	public void SelectionOrPosition_Shows_The_Selection_Instead_Of_The_Position()
	{
		// Arrange
		DocumentStatus status = default(DocumentStatus) with
		{
			CaretOffset = 776,
			SelectionLength = 128,
			SelectionLineCount = 45
		};

		// Act
		object? result = Convert(AppConverters.SelectionOrPosition, status);

		// Assert
		result
			.Should()
			.BeOfType<string>()
			.Which
			.Should()
			.Contain("128")
			.And
			.Contain("45")
			.And
			.NotContain("777");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Runs a converter over the time left of a countdown.
	/// </summary>
	private static object? Convert(IValueConverter converter, TimeSpan? remaining)
	{
		return converter.Convert(
			remaining,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over the line endings of a document.
	/// </summary>
	private static object? Convert(FuncValueConverter<LineEnding, string?> converter, LineEnding ending)
	{
		return converter.Convert(
			ending,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over a number with the format given as the parameter.
	/// </summary>
	private static object? Convert(FuncValueConverter<int, string, string> converter, int number, string format)
	{
		return converter.Convert(
			number,
			typeof(object),
			format,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over the status of a document.
	/// </summary>
	private static object? Convert(FuncValueConverter<DocumentStatus, string> converter, DocumentStatus status)
	{
		return converter.Convert(
			status,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}
	#endregion
}
